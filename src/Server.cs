using System.Net;
using System.Net.Sockets;

// You can use print statements as follows for debugging, they'll be visible when running tests.
Console.WriteLine("Logs from your program will appear here!");

// Uncomment this block to pass the first stage
TcpListener server = new TcpListener(IPAddress.Any, 6379);
server.Start();

using Socket client = server.AcceptSocket(); // wait for client

if (client.Connected) { 
    Console.WriteLine("Connected");

    //Send back a response
    var response = "+PONG\r\n";
    byte[] msg = System.Text.Encoding.ASCII.GetBytes(response);
    await client.SendAsync(msg);
}
