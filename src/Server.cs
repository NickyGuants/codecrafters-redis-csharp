using System.Net;
using System.Net.Sockets;
using System.Text;

namespace codecrafters_redis.src
{
    public class Server
    {
        private TcpListener _server;
        public static Dictionary<string, (string, DateTime?)> data = new Dictionary<string, (string, DateTime?)>();
        public static string RDBFileDirectory=string.Empty;
        public static string RDBFileName = string.Empty;

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
                                if (decodedParts.Length > 3 && decodedParts[3].Equals("px", StringComparison.OrdinalIgnoreCase))
                                {
                                    var ms = Convert.ToDouble(decodedParts[4]);
                                    data.Add(decodedParts[1], (decodedParts[2], DateTime.UtcNow.AddMilliseconds(ms)));
                                }else{
                                    data.Add(decodedParts[1], (decodedParts[2], null));
                                }
                                response = "+OK\r\n";
                                break;
                            case "GET":
                                var key = decodedParts[1];

                                if (data.ContainsKey(key))
                                {
                                    var (value, expiryTime) = data[key];
                                    if (expiryTime !=null && expiryTime <= DateTime.UtcNow)
                                    {
                                        data.Remove(key);
                                        data.Remove(key);
                                        response = $"$-1\r\n";
                                    }
                                    else{
                                        response = $"+{value}\r\n";
                                    }
                                    break;
                                }
                                response = $"$-1\r\n";
                                break;
                            
                            case "CONFIG":
                                if(decodedParts[2] == "dir"){
                                    response = $"*2\r\n$3\r\ndir\r\n${RDBFileDirectory.Length}\r\n{RDBFileDirectory}\r\n";
                            
                                }else if(decodedParts[2] == "dbfilename"){
                                    response = $"*2\r\n$10\r\ndbfilename\r\n${RDBFileName.Length}\r\n{RDBFileName}\r\n";
                                   
                                }else{
                                    response = $"-ERR unknown command '{command}'\r\n";
                                    
                                }
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
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--dir":
                        if (i + 1 < args.Length) RDBFileDirectory = args[++i];
                        break;
                    case "--dbfilename":
                        if (i + 1 < args.Length) RDBFileName = args[++i];
                        break;
                }
            }

            Server server = new Server(IPAddress.Any, 6379);
            server.Start();
        }
    }
}
