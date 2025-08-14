using System.Net;
using System.Net.Sockets;
using Redis.Common;
using Redis.Receivers;

namespace Redis.Nodes;

public class ReplicaNode(IPAddress localAddress, int port, string? masterNode, int? masterPort)
    : NodeBase(localAddress, port, new ReplicaReceiver())
{
    private readonly int port = port;

    protected override void LogOnStart() => Console.WriteLine($"starting Redis '{NodeName}' server on port '{port}'");
    protected sealed override string NodeName => $"replica-node-{port}";

    public ReplicaNode Handshake()
    {
        try
        {
            var tcpClient = masterNode == null || masterPort == null
                ? throw new Exception("Handshake failed: master node is not specified")
                : new TcpClient(masterNode, masterPort.Value);

            tcpClient.GetStream()
                .SendPing()
                .SendReplconfListeningPort(port)
                .SendReplconfCapaPsync2()
                .SendPsync();

            ServerInfo.Replication.ReplicaHandshakeCompleted = true;
            ServerInfo.ServerRuntimeContext.MasterSocket = tcpClient.Client;
            
            Task.Run(() => { _ = HandleConnection(tcpClient); });
            
            Console.WriteLine($"[{NodeName}] Handshake completed");
            return this;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Handshake failed: {ex.Message}, stack: {ex.StackTrace}");
            throw;
        }
    }
}