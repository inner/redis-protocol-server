using System.Net;
using System.Net.Sockets;
using codecrafters_redis;
using codecrafters_redis.Receivers;
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

try
{
    if (ServerInfo.IsMaster)
    {
        new MasterNode(IPAddress.Any, port, new MasterReceiver())
            .Start();
    }
    else
    {
        if (!IsMasterNodePortOpen(masterHost!, masterPort!.Value))
        {
            new MasterNode(IPAddress.Any, masterPort.Value, new MasterReceiver())
                .Start();
        }

        new ReplicaNode(IPAddress.Any, port, masterHost!, masterPort!.Value, new ReplicaReceiver())
            .Handshake()
            .Start();
    }
}
catch (Exception ex)
{
    Console.WriteLine($"{ex.Message}, stack: {ex.StackTrace}");
    throw;
}

bool IsMasterNodePortOpen(string host, int portNumber)
{
    try
    {
        using var tcpClient = new TcpClient();
        tcpClient.Connect(host, portNumber);
        return true;
    }
    catch (SocketException)
    {
        return false;
    }
}