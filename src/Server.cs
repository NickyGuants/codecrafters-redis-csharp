using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Timers;

namespace codecrafters_redis.src
{
    public class Server
    {
        private TcpListener _server;
        public static Dictionary<string, string> keyValues = new Dictionary<string, string>();
        public static Dictionary<string, DateTime> keyExpiry = new Dictionary<string, DateTime>();
        private static System.Timers.Timer timer;

        public Server(IPAddress iPAddress, int port)
        {
            _server = new TcpListener(iPAddress, port);
        }

        public void Start()
        {
            try
            {
                _server.Start();
                Console.WriteLine("Redis Server Started");

                while (true)
                {
                    Socket client = _server.AcceptSocket(); // wait for client
                    Console.WriteLine("Client Connected");

                    _ = Task.Run(() => HandleClient(client));
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"SocketException: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
            }
        }

        public async Task HandleClient(Socket client)
        {
            int bytesRead = 0;
            Byte[] buffer = new byte[1024];
            StringBuilder stringBuilder = new StringBuilder();

            try
            {
                while ((bytesRead = client.Receive(buffer)) > 0)
                {
                    var inputChunk = System.Text.Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    stringBuilder.Append(inputChunk);

                    //Check if we have a complete RESP message ie ends with \r\n
                    if(inputChunk.EndsWith("\r\n")){
                        string input = stringBuilder.ToString();
                        string[] decodedParts = RespParser.Decode(input).Split(',');

                        string command = decodedParts[0].ToUpper();
                        string response;
                        switch(command)
                        {
                            case "PING":
                                response = "+PONG\r\n";
                                break;
                            case "ECHO":
                                if(decodedParts.Length <2){
                                    response = "-ERR wrong number of arguments for 'echo' Command\r\n";
                                }

                                response = $"+{decodedParts[1]}\r\n";
                                break;
                            case "SET":
                                keyValues.Add(decodedParts[1], decodedParts[2]);
                                if (decodedParts.Length>3 && decodedParts[3].Equals("px", StringComparison.OrdinalIgnoreCase))
                                {
                                    var ms = Convert.ToDouble(decodedParts[4]);
                                    keyExpiry.Add(decodedParts[1], DateTime.UtcNow.AddMilliseconds(ms));
                                }
                                response = "+OK\r\n";
                                break;
                            case "GET":
                                var key = decodedParts[1];

                                if (keyValues.TryGetValue(key, out var value))
                                {
                                    if (keyExpiry.TryGetValue(key, out var expiryTime) && expiryTime <= DateTime.UtcNow)
                                    {
                                        keyValues.Remove(key);
                                        keyExpiry.Remove(key);
                                        response = $"$-1\r\n";
                                    }
                                    else{
                                        response = $"+{value}\r\n";
                                    }
                                    break;
                                }
                                response = $"$-1\r\n";
                                break;
                            default:
                                response = $"-ERR unknown command '{command}'\r\n";
                                break;
                            
                        }
                    
                        //Send back a response
                        byte[] msg = System.Text.Encoding.ASCII.GetBytes(response);
                        await client.SendAsync(msg);
                        stringBuilder.Clear();
                    }
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"SocketException: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
            }
            finally
            {
                client.Close();
            }
        }

        public static void Main(string[] args)
        {
            // You can use print statements as follows for debugging, they'll be visible when running tests.
            Console.WriteLine("Logs from your program will appear here!");

            Server server = new Server(IPAddress.Any, 6379);
            server.Start();
        }

        private static void OnTimedEvent(Object source, ElapsedEventArgs e){
            Console.WriteLine("Emitted");
        }
    }
}
