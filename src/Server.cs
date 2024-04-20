using System.Net;
using System.Net.Sockets;
using System.Text;
using codecrafters_redis;

Console.WriteLine("Logs from your program will appear here!");

var server = new TcpListener(IPAddress.Any, 6379);
server.Start();

// var respClientCommandExecutor = new RespClientCommandExecutor();

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
                var respClientCommandString = Encoding.UTF8.GetString(buffer, 0, data);
                Console.WriteLine($"[{connectionId}] received: {respClientCommandString}");

                var commands = respClientCommandString.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var response = $"${commands[1].Length}\r\n{commands[1]}\r\n";
                
                // var respCommandType = respClientCommandString.GetRespClientCommandType();
                // var respResponse = respClientCommandExecutor.Execute(respCommandType, respClientCommandString);
                // socket.Send("+PONG\r\n"u8.ToArray());
                
                socket.Send(Encoding.UTF8.GetBytes(response));
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