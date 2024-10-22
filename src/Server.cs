using System.Net;
using System.Net.Sockets;

public class Server
{
    private TcpListener _server;

    public Server(IPAddress iPAddress, int port){
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

    public async Task HandleClient(Socket client){
        int i = 0;
        Byte[] bytes = new byte[256];

        try
        {
            while ((i = client.Receive(bytes)) != 0)
            {
                //Send back a response
                var response = "+PONG\r\n";
                byte[] msg = System.Text.Encoding.ASCII.GetBytes(response);
                await client.SendAsync(msg);
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

    public static void Main(string[] args){
        // You can use print statements as follows for debugging, they'll be visible when running tests.
        Console.WriteLine("Logs from your program will appear here!");

        Server server = new Server(IPAddress.Any, 6379);
        server.Start();
    }
}