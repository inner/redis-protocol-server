using System.Net;
using System.Net.Sockets;
using System.Text;

Console.WriteLine("Logs from your program will appear here!");

var server = new TcpListener(IPAddress.Any, 6379);
server.Start();

while (true)
{
    server.BeginAcceptSocket(ConnectionCallback, server);
}

void ConnectionCallback(IAsyncResult asyncResult)
{
    var socket = server.EndAcceptSocket(asyncResult);
    Console.WriteLine($"TCP Connection [{socket.LocalEndPoint}->{socket.RemoteEndPoint}] established!");

    var buffer = new StringBuilder();
    var tempBuffer = new byte[1024];
    int bytesRead;

    do
    {
        bytesRead = socket.Receive(tempBuffer);
        buffer.Append(Encoding.ASCII.GetString(tempBuffer, 0, bytesRead));
    } while (bytesRead == tempBuffer.Length);

    var clientCommand = buffer.ToString();
    socket.Send(Encoding.ASCII.GetBytes("+PONG\\r\\n"));
    socket.Close();
}