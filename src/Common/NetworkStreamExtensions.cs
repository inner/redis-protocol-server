using System.Net.Sockets;
using System.Text;

namespace codecrafters_redis.Common;

public static class NetworkStreamExtensions
{
    public static NetworkStream SendPing(this NetworkStream stream)
    {
        var resp = RespBuilder.BuildRespArray("PING");
        stream.Write(resp);

        EnsureExpectedResponse(
            nameof(SendPing),
            Constants.PongResponse,
            stream.ReadResponse());

        return stream;
    }

    public static NetworkStream SendReplconfListeningPort(this NetworkStream stream, int port)
    {
        var resp = RespBuilder.BuildRespArray("REPLCONF", "listening-port", port.ToString());
        stream.Write(resp);

        EnsureExpectedResponse(
            nameof(SendReplconfListeningPort),
            Constants.OkResponse,
            stream.ReadResponse());

        return stream;
    }

    public static NetworkStream SendReplconfCapaPsync2(this NetworkStream stream)
    {
        var resp = RespBuilder.BuildRespArray("REPLCONF", "capa", "psync2");
        stream.Write(resp);

        EnsureExpectedResponse(
            nameof(SendReplconfCapaPsync2),
            Constants.OkResponse,
            stream.ReadResponse());

        return stream;
    }

    public static void SendPsync(this NetworkStream stream)
    {
        // e.g. PSYNC ? -1
        var resp = RespBuilder.BuildRespArray("PSYNC", "?", "-1");
        stream.Write(resp);

        // read FULLRESYNC response
        // e.g., +FULLRESYNC <replication_id> <offset>
        var fullResyncResponse = ReadLine(stream)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();

        if (!fullResyncResponse.StartsWith("+FULLRESYNC"))
        {
            throw new Exception("Expected FULLRESYNC response, but received: " + fullResyncResponse);
        }

        // read the RDB length
        // e.g. $<length>
        var rdbLengthStr = ReadLine(stream)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();

        if (!rdbLengthStr.StartsWith("$"))
        {
            throw new Exception("Expected RDB length, but received: " + rdbLengthStr);
        }

        var rdbLength = int.Parse(rdbLengthStr.Substring(1));
        var rdbFile = new byte[rdbLength];

        // read the RDB file
        var bytesRead = 0;
        while (bytesRead < rdbLength)
        {
            bytesRead += stream.ReadAsync(rdbFile, bytesRead, rdbLength - bytesRead)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }

        // Process the RDB file (e.g., save it, load it, etc.)

        Console.WriteLine("Received RDB file of length: " + rdbLength);
    }

    private static async Task<string> ReadLine(NetworkStream stream)
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

            var @char = (char)buffer[0];
            if (@char == '\r')
            {
                // expecting \n after \r
                bytesRead = await stream.ReadAsync(buffer, 0, 1);
                if (bytesRead == 0 || buffer[0] != '\n')
                {
                    throw new Exception("Malformed response.");
                }

                break;
            }

            sb.Append(@char);
        }

        return sb.ToString();
    }

    public static string ReadResponse(this NetworkStream stream)
    {
        using var memoryStream = new MemoryStream();
        var buffer = new byte[1024];
        int bytesRead;

        while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            memoryStream.Write(buffer, 0, bytesRead);
            if (bytesRead < buffer.Length)
            {
                break;
            }
        }

        return Encoding.UTF8.GetString(memoryStream.ToArray());
    }

    private static void EnsureExpectedResponse(string methodName, string expectedResponse, string actualResponse)
    {
        if (!string.Equals(expectedResponse, actualResponse, StringComparison.InvariantCultureIgnoreCase))
        {
            throw new Exception($"Handshake failed on step: {methodName}. " +
                                $"Expected response: {expectedResponse}, but received: {actualResponse}");
        }
    }
}