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
    var connectionId = $"{socket.LocalEndPoint}->{socket.RemoteEndPoint}";
    
    while (socket.Connected)
    {
        try
        {
            Console.WriteLine($"TCP Connection [{connectionId}] established!");

            var buffer = new byte[1024];
            var data = socket.Receive(buffer);

            while (data > 0)
            {
                Console.WriteLine($"[{connectionId}] received: {Encoding.ASCII.GetString(buffer, 0, data).Trim()}");
                socket.Send(Encoding.ASCII.GetBytes("+PONG\r\n"));
                data = socket.Receive(buffer);
            }
        }
        catch (SocketException)
        {
            Console.WriteLine($"Closing TCP connection: [{connectionId}].");
        }
        finally
        {
            CloseSocket(connectionId, socket);
        }
    }
}

void CloseSocket(string connectionId, Socket? socket)
{
    if (socket == null)
    {
        return;
    }
    
    Console.WriteLine($"TCP Connection [{connectionId}] closed.");
    socket.Close();
}