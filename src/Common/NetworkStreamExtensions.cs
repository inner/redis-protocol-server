using System.Net.Sockets;
using System.Text;

namespace codecrafters_redis.Common;

public static class NetworkStreamExtensions
{
    public static NetworkStream SendPing(this NetworkStream stream)
    {
        var ping = RespBuilder.BuildRespArray("PING");

        StreamWrite(stream, ping);
        if (StreamRead(stream) != Constants.PongResponse)
        {
            ThrowHandshakeFailed(nameof(SendPing));
        }

        return stream;
    }

    public static NetworkStream SendReplconfListeningPort(this NetworkStream stream, string nodeName, int port)
    {
        Console.WriteLine($"[{nodeName}] Sending REPLCONF listening-port {port}");

        var replconfListeningPort = RespBuilder.BuildRespArray("REPLCONF", "listening-port", port.ToString());

        StreamWrite(stream, replconfListeningPort);
        if (StreamRead(stream) != Constants.OkResponse)
        {
            ThrowHandshakeFailed(nameof(SendReplconfListeningPort));
        }

        return stream;
    }

    public static NetworkStream SendReplconfCapaPsync2(this NetworkStream stream)
    {
        var replconfCapa = RespBuilder.BuildRespArray("REPLCONF", "capa", "psync2");

        StreamWrite(stream, replconfCapa);
        if (StreamRead(stream) != Constants.OkResponse)
        {
            ThrowHandshakeFailed(nameof(SendReplconfCapaPsync2));
        }

        return stream;
    }

    public static void SendPsync(this NetworkStream stream)
    {
        var replconfPsync = RespBuilder.BuildRespArray("PSYNC", "?", "-1");
        StreamWrite(stream, replconfPsync);

        // Read FULLRESYNC response
        var fullResyncResponse = ReadLineAsync(stream)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();

        Console.WriteLine("Received: " + fullResyncResponse);

        if (fullResyncResponse.StartsWith("+FULLRESYNC"))
        {
            // Read the RDB length
            var rdbLengthStr = ReadLineAsync(stream)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();

            if (rdbLengthStr.StartsWith("$"))
            {
                var rdbLength = int.Parse(rdbLengthStr.Substring(1));
                var rdbFile = new byte[rdbLength];

                // Read the RDB file
                var bytesRead = 0;
                while (bytesRead < rdbLength)
                {
                    var read = stream.ReadAsync(rdbFile, bytesRead, rdbLength - bytesRead)
                        .ConfigureAwait(false)
                        .GetAwaiter()
                        .GetResult();

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

    private static async Task<string> ReadLineAsync(NetworkStream stream)
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