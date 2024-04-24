using System.Net;
using System.Net.Sockets;
using System.Text;
using codecrafters_redis;

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
ServerInfo.MasterReplId = GenerateRandomReplId();

var serverType = ServerInfo.IsMaster
    ? "master"
    : "slave";

Console.WriteLine($"Starting Redis '{serverType}' server");

var server = new TcpListener(IPAddress.Any, port);
server.Start();

var receiver = new Receiver();

while (true)
{
    var socket = server.AcceptSocket();
    _ = Task.Run(() => HandleConnection(socket));
}

void HandleConnection(Socket socket)
{
    var connectionId = $"{socket.LocalEndPoint}->{socket.RemoteEndPoint}";

    while (socket.Connected)
    {
        try
        {
            Console.WriteLine($"TCP Connection [{connectionId}] established");

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
            Console.WriteLine($"Closing TCP connection: [{connectionId}]");
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

    Console.WriteLine($"TCP Connection [{connectionId}] closed");
    socket.Close();
}

void LogReceivedMessage(string s, string respClientCommandString)
{
    var receivedMessage = $"[{s}] received: \"{respClientCommandString.Replace("\r\n", @"\r\n")}\"";
    Console.WriteLine(receivedMessage[..^1][..^1]);
}

string GenerateRandomReplId()
{
    var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    var random = new Random();
    var result = new string(
        Enumerable.Repeat(chars, 40)
            .Select(s => s[random.Next(s.Length)])
            .ToArray()
    );

    return result;
}