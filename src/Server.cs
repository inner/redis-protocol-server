using System.Net;
using System.Net.Sockets;
using System.Text;
using codecrafters_redis;

Console.WriteLine("Logs from your program will appear here!");

var server = new TcpListener(IPAddress.Any, 6379);
server.Start();

var clientCommandExecutor = new ClientCommandExecutor();

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

            while (true)
            {
                var buffer = new byte[1024];
                var data = socket.Receive(buffer);

                var clientCommandString = Encoding.UTF8.GetString(buffer, 0, data);
                
                if (string.IsNullOrWhiteSpace(clientCommandString))
                {
                    socket.Send(Encoding.UTF8.GetBytes(Environment.NewLine));
                    continue;
                }
                
                Console.WriteLine($"[{connectionId}] received: {clientCommandString}");
                var response = clientCommandExecutor.Execute(clientCommandString);
                socket.Send(Encoding.UTF8.GetBytes(response));
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

    if (asyncResult.IsCompleted)
    {
        Console.WriteLine("Connection closed.");
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