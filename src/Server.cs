using System.Net;
using System.Net.Sockets;
using System.Text;
using codecrafters_redis;

Console.WriteLine("Starting Redis server.");

var port = args.Length > 0 && (args[0] == "--port" || args[0] == "-p")
    ? int.Parse(args[1])
    : Constants.DefaultRedisPort;

var masterHost = args.Length > 2 && args[2] == "--replicaof"
    ? args[3]
    : null;

int? masterPort = args.Length > 2 && args[2] == "--replicaof"
    ? int.Parse(args[4])
    : null;

var isMaster = masterHost == null;

ServerInfo.Port = port;
ServerInfo.IsMaster = isMaster;
ServerInfo.MasterHost = masterHost;
ServerInfo.MasterPort = masterPort;

var server = new TcpListener(IPAddress.Any, port);
server.Start();

Console.WriteLine("Server started.");

var receiver = new Receiver();

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

                var respClientCommandString = Encoding.UTF8.GetString(buffer, 0, data);

                if (string.IsNullOrWhiteSpace(respClientCommandString))
                {
                    socket.Send(Encoding.UTF8.GetBytes(Environment.NewLine));
                    continue;
                }

                LogReceivedMessage(connectionId, respClientCommandString);

                var response = receiver.Receive(respClientCommandString);
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

void LogReceivedMessage(string s, string respClientCommandString)
{
    var receivedMessage =
        $"[{s}] received: \"{respClientCommandString
            .Replace("\\\\r\\\\n", "\\r\\n")
            .Replace("\n", string.Empty)}\"";

    Console.WriteLine(receivedMessage);
}