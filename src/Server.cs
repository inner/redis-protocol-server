using System.Net;
using codecrafters_redis;
using codecrafters_redis.Servers;

var port = args.Length > 0 && (args[0] == "--port" || args[0] == "-p")
    ? int.Parse(args[1])
    : Constants.DefaultRedisPort;

var masterHost = args.Length > 2 && args[2] == "--replicaof"
    ? args[3]
    : null;

int? masterPort = args.Length > 2 && args[2] == "--replicaof"
    ? int.Parse(args[4])
    : null;

ServerInfo.IsMaster = masterHost == null;

// var server = new TcpListener(IPAddress.Any, port);
// server.Start();
//
// if (!isMaster)
// {
//     var handshake = new Handshake(
//         ServerInfo.MasterHost!,
//         ServerInfo.MasterPort!.Value);
//
//     handshake.Start();
// }

if (ServerInfo.IsMaster)
{
    ServerInfo.MasterReplId = GenerateRandomReplId();
    ServerInfo.MasterReplOffset = 0;
    
    new MasterNode(IPAddress.Any, port, new Receiver())
        .Start();
}
else
{
    new ReplicaNode(IPAddress.Any, port, masterHost!, masterPort!.Value, new Receiver())
        .Handshake()
        .Start();
}

// while (true)
// {
//     var socket = server.AcceptSocket();
//     _ = Task.Run(() => HandleConnection(socket));
// }
//
// void HandleConnection(Socket socket)
// {
//     var connectionId = $"{socket.LocalEndPoint}->{socket.RemoteEndPoint}";
//
//     while (socket.Connected)
//     {
//         try
//         {
//             Console.WriteLine($"TCP Connection [{connectionId}] established");
//
//             while (true)
//             {
//                 var buffer = new byte[1024];
//                 var bytesReceived = socket.Receive(buffer);
//                 var clientCommand = Encoding.UTF8.GetString(buffer, 0, bytesReceived);
//
//                 LogReceivedCommand(clientCommand);
//
//                 if (string.IsNullOrWhiteSpace(clientCommand))
//                 {
//                     socket.Send(Encoding.UTF8.GetBytes(Environment.NewLine));
//                     continue;
//                 }
//
//                 receiver.Receive(socket, clientCommand);
//             }
//         }
//         catch (SocketException)
//         {
//             Console.WriteLine($"Closing TCP connection: [{connectionId}]");
//         }
//         finally
//         {
//             CloseSocket(connectionId, socket);
//         }
//     }
// }
//
// void CloseSocket(string connectionId, Socket? socket)
// {
//     if (socket == null)
//     {
//         return;
//     }
//
//     Console.WriteLine($"TCP Connection [{connectionId}] closed");
//     socket.Close();
// }

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

// void LogReceivedCommand(string s)
// {
//     var logMessage = s.Replace("\r\n", "\\r\\n");
//
//     if (!logMessage.EndsWith('\n'))
//     {
//         logMessage += '\n';
//     }
//     
//     Console.Write($"Received command: {logMessage}");
// }