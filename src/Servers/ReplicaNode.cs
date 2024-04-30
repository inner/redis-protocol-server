using System.Net;
using System.Net.Sockets;
using System.Text;

namespace codecrafters_redis.Servers;

public class ReplicaNode : NodeBase
{
    private readonly int port;
    private readonly TcpClient tcpClient;

    public ReplicaNode(IPAddress localAddress, int port, string masterNode, int masterPort, Receiver receiver)
        : base(localAddress, port, receiver)
    {
        this.port = port;
        tcpClient = new TcpClient(masterNode, masterPort);
    }

    protected override void LogOnStart()
    {
        Console.WriteLine($"starting Redis 'replica' server on port '{port}'");
    }

    protected override string NodeName => $"replica-node-{port}";

    public ReplicaNode Handshake()
    {
        var stream = tcpClient.GetStream();

        SendPing(stream);
        SendReplconfListeningPort(stream);
        SendReplconfCapaPsync2(stream);
        SendPsync(stream);

        ServerInfo.ReplicaHandshakeCompleted = true;
        
        Task.Run(() => HandleConnection(tcpClient.Client));
        
        return this;
    }

    private void SendPing(NetworkStream stream)
    {
        const string ping = "*1\r\n$4\r\nPING\r\n";
        StreamWrite(stream, ping);
        
        if (StreamRead(stream) != Constants.PongResponse)
        {
            ThrowHandshakeFailed();
        }
    }
    
    private void SendReplconfListeningPort(NetworkStream stream)
    {
        var listeningPortString = port.ToString();
        var replconfListeningPort = $"*3\r\n$8\r\nREPLCONF\r\n$14\r\nlistening-port\r\n${listeningPortString.Length}\r\n{listeningPortString}\r\n";
        StreamWrite(stream, replconfListeningPort);
        
        if (StreamRead(stream) != Constants.OkResponse)
        {
            ThrowHandshakeFailed();
        }
    }
    
    private void SendReplconfCapaPsync2(NetworkStream stream)
    {
        const string replconfCapa = "*3\r\n$8\r\nREPLCONF\r\n$4\r\ncapa\r\n$6\r\npsync2\r\n";
        StreamWrite(stream, replconfCapa);
        
        if (StreamRead(stream) != Constants.OkResponse)
        {
            ThrowHandshakeFailed();
        }
    }
    
    private void SendPsync(NetworkStream stream)
    {
        const string replconfPsync = "*3\r\n$5\r\nPSYNC\r\n$1\r\n?\r\n$2\r\n-1\r\n";
        StreamWrite(stream, replconfPsync);
        
        if (!StreamRead(stream).Contains("FULLRESYNC"))
        {
            ThrowHandshakeFailed();
        }
    }

    private static void ThrowHandshakeFailed()
    {
        throw new Exception("Handshake failed");
    }

    private static string StreamRead(NetworkStream stream)
    {
        var buffer = new byte[1024];
        var bytesRead = stream.Read(buffer, 0, buffer.Length);
        var response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        
        return response;
    }

    private static void StreamWrite(NetworkStream stream, string ping)
    {
        var bytes = Encoding.UTF8.GetBytes(ping);
        stream.Write(bytes, 0, bytes.Length);
    }
}