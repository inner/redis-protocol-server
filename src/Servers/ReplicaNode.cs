using System.Net;
using System.Net.Sockets;
using System.Text;
using codecrafters_redis.Common;
using codecrafters_redis.Receivers;

namespace codecrafters_redis.Servers;

public class ReplicaNode(IPAddress localAddress, int port, string? masterNode, int? masterPort, ReceiverBase receiver)
    : NodeBase(localAddress, port, receiver)
{
    private readonly int port = port;
    private TcpClient? tcpClient;

    protected override void LogOnStart() => Console.WriteLine($"starting Redis '{NodeName}' server on port '{port}'");
    protected sealed override string NodeName => $"replica-node-{port}";

    public ReplicaNode Handshake()
    {
        try
        {
            tcpClient = masterNode == null || !masterPort.HasValue
                ? null
                : new TcpClient(masterNode, masterPort.Value);

            if (tcpClient == null)
            {
                Console.WriteLine("TCP client is null. Exiting...");
                return this;
            }

            var stream = tcpClient.GetStream();

            SendPing(stream);
            SendReplconfListeningPort(stream);
            SendReplconfCapaPsync2(stream);
            SendPsync(stream).Wait();

            ServerInfo.Replication.ReplicaHandshakeCompleted = true;

            Console.WriteLine($"[{NodeName}] Handshake completed");

            Task.Run(() => { _ = HandleConnection(tcpClient); });

            return this;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Handshake failed: {ex.Message}, stack: {ex.StackTrace}");
            throw;
        }
    }

    private void SendPing(NetworkStream stream)
    {
        var ping = RespBuilder.BuildRespArray("PING");
        
        StreamWrite(stream, ping);
        if (StreamRead(stream) != Constants.PongResponse)
        {
            ThrowHandshakeFailed(nameof(SendPing));
        }
    }

    private void SendReplconfListeningPort(NetworkStream stream)
    {
        Console.WriteLine($"[{NodeName}] Sending REPLCONF listening-port {port}");

        var replconfListeningPort = RespBuilder.BuildRespArray("REPLCONF", "listening-port", port.ToString());
        
        StreamWrite(stream, replconfListeningPort);
        if (StreamRead(stream) != Constants.OkResponse)
        {
            ThrowHandshakeFailed(nameof(SendReplconfListeningPort));
        }
    }

    private void SendReplconfCapaPsync2(NetworkStream stream)
    {
        var replconfCapa = RespBuilder.BuildRespArray("REPLCONF", "capa", "psync2");
        
        StreamWrite(stream, replconfCapa);
        if (StreamRead(stream) != Constants.OkResponse)
        {
            ThrowHandshakeFailed(nameof(SendReplconfCapaPsync2));
        }
    }

    private async Task SendPsync(NetworkStream stream)
    {
        var replconfPsync = RespBuilder.BuildRespArray("PSYNC", "?", "-1");
        StreamWrite(stream, replconfPsync);

        // Read FULLRESYNC response
        var fullResyncResponse = await ReadLineAsync(stream);
        Console.WriteLine("Received: " + fullResyncResponse);

        if (fullResyncResponse.StartsWith("+FULLRESYNC"))
        {
            // Read the RDB length
            var rdbLengthStr = await ReadLineAsync(stream);
            if (rdbLengthStr.StartsWith("$"))
            {
                var rdbLength = int.Parse(rdbLengthStr.Substring(1));
                var rdbFile = new byte[rdbLength];

                // Read the RDB file
                var bytesRead = 0;
                while (bytesRead < rdbLength)
                {
                    var read = await stream.ReadAsync(rdbFile, bytesRead, rdbLength - bytesRead);
                    if (read == 0)
                    {
                        throw new Exception("Unexpected end of stream while reading RDB file.");
                    }

                    bytesRead += read;
                }

                Console.WriteLine("Received RDB file of length: " + rdbLength);

                // Process the RDB file (e.g., save it, load it, etc.)
            }
        }
    }

    static async Task<string> ReadLineAsync(NetworkStream stream)
    {
        var sb = new StringBuilder();
        var buffer = new byte[1];
        while (true)
        {
            var bytesRead = await stream.ReadAsync(buffer, 0, 1);
            if (bytesRead == 0)
            {
                throw new Exception("Unexpected end of stream.");
            }

            var ch = (char)buffer[0];
            if (ch == '\r')
            {
                // Expecting \n after \r
                bytesRead = await stream.ReadAsync(buffer, 0, 1);
                if (bytesRead == 0 || buffer[0] != '\n')
                {
                    throw new Exception("Malformed response.");
                }

                break;
            }

            sb.Append(ch);
        }

        return sb.ToString();
    }

    private static void ThrowHandshakeFailed(string failedMethodName)
    {
        throw new Exception($"Handshake failed on step: {failedMethodName}");
    }

    private static string StreamRead(NetworkStream stream)
    {
        var buffer = new byte[1024];
        var bytesRead = stream.Read(buffer, 0, buffer.Length);
        var response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

        return response;
    }

    private static void StreamWrite(NetworkStream stream, string command)
    {
        var bytes = Encoding.UTF8.GetBytes(command);
        stream.Write(bytes, 0, bytes.Length);
    }
}