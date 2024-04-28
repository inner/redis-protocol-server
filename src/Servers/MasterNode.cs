using System.Net;

namespace codecrafters_redis.Servers;

public class MasterNode : NodeBase
{
    private readonly int port;

    public MasterNode(IPAddress localAddress, int port, Receiver receiver)
        : base(localAddress, port, receiver)
    {
        this.port = port;
    }

    protected override void LogOnStart()
    {
        Console.WriteLine($"Starting Redis 'master' server on port '{port}'");
    }
}