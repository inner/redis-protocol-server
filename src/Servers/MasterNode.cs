using System.Net;
using codecrafters_redis.Receivers;

namespace codecrafters_redis.Servers;

public class MasterNode : NodeBase
{
    private readonly int port;

    public MasterNode(IPAddress localAddress, int port)
        : base(localAddress, port, new MasterReceiver())
    {
        SetServerInfo();
        this.port = port;
    }

    protected override void LogOnStart()
    {
        Console.WriteLine($"starting Redis 'master' server on port '{port}'");
    }

    protected override string NodeName => "master-node";

    private static void SetServerInfo()
    {
        ServerInfo.ServerRuntimeContext.MasterReplId = GenerateRandomReplId();
    }

    private static string GenerateRandomReplId()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        var result = new string(
            Enumerable.Repeat(chars, 40)
                .Select(s => s[random.Next(s.Length)])
                .ToArray()
        );

        return result;
    }
}