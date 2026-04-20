using System.Net.Sockets;
using System.Text;

namespace Redis.Tests.Support;

internal sealed class RedisRespClient : IAsyncDisposable
{
    private readonly TcpClient tcpClient;
    private readonly NetworkStream stream;

    private RedisRespClient(TcpClient tcpClient)
    {
        this.tcpClient = tcpClient;
        stream = tcpClient.GetStream();
    }

    public static async Task<RedisRespClient> ConnectAsync(
        string hostname,
        int port,
        CancellationToken cancellationToken = default)
    {
        var tcpClient = new TcpClient();
        using var connectTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        connectTimeout.CancelAfter(TimeSpan.FromSeconds(2));
        await tcpClient.ConnectAsync(hostname, port, connectTimeout.Token);
        return new RedisRespClient(tcpClient);
    }

    public async Task<string> ExecuteAsync(
        IReadOnlyList<string> commandParts,
        CancellationToken cancellationToken = default)
    {
        var request = Encoding.UTF8.GetBytes(RespBuilder.ArrayFromCommands(commandParts.ToArray()));
        await stream.WriteAsync(request, cancellationToken);
        await stream.FlushAsync(cancellationToken);
        return await ReadResponseAsync(cancellationToken);
    }

    public Task<string> ExecuteCommandAsync(params string[] commandParts)
    {
        return ExecuteAsync((IReadOnlyList<string>)commandParts, CancellationToken.None);
    }

    public async ValueTask DisposeAsync()
    {
        await stream.DisposeAsync();
        tcpClient.Dispose();
    }

    private async Task<string> ReadResponseAsync(CancellationToken cancellationToken)
    {
        var prefix = (char)await ReadByteAsync(cancellationToken);

        return prefix switch
        {
            '+' or '-' or ':' => $"{prefix}{await ReadLineAsync(cancellationToken)}\r\n",
            '$' => await ReadBulkStringAsync(prefix, cancellationToken),
            '*' => await ReadArrayAsync(prefix, cancellationToken),
            _ => throw new InvalidOperationException($"Unsupported RESP prefix '{prefix}'.")
        };
    }

    private async Task<string> ReadBulkStringAsync(char prefix, CancellationToken cancellationToken)
    {
        var lengthText = await ReadLineAsync(cancellationToken);
        var length = int.Parse(lengthText);
        if (length == -1)
        {
            return $"{prefix}{length}\r\n";
        }

        var body = await ReadExactlyAsync(length + 2, cancellationToken);
        return $"{prefix}{length}\r\n{Encoding.UTF8.GetString(body)}";
    }

    private async Task<string> ReadArrayAsync(char prefix, CancellationToken cancellationToken)
    {
        var countText = await ReadLineAsync(cancellationToken);
        var count = int.Parse(countText);
        if (count == -1)
        {
            return $"{prefix}{count}\r\n";
        }

        var builder = new StringBuilder($"{prefix}{count}\r\n");
        for (var i = 0; i < count; i++)
        {
            builder.Append(await ReadResponseAsync(cancellationToken));
        }

        return builder.ToString();
    }

    private async Task<string> ReadLineAsync(CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();

        while (true)
        {
            var current = await ReadByteAsync(cancellationToken);
            if (current == '\r')
            {
                var lineFeed = await ReadByteAsync(cancellationToken);
                if (lineFeed != '\n')
                {
                    throw new InvalidOperationException("Malformed RESP line ending.");
                }

                return builder.ToString();
            }

            builder.Append((char)current);
        }
    }

    private async Task<byte[]> ReadExactlyAsync(int length, CancellationToken cancellationToken)
    {
        var buffer = new byte[length];
        var totalRead = 0;

        while (totalRead < length)
        {
            var bytesRead = await stream.ReadAsync(
                buffer.AsMemory(totalRead, length - totalRead),
                cancellationToken);

            if (bytesRead == 0)
            {
                throw new InvalidOperationException("Unexpected end of stream.");
            }

            totalRead += bytesRead;
        }

        return buffer;
    }

    private async Task<byte> ReadByteAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[1];
        var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, 1), cancellationToken);
        if (bytesRead == 0)
        {
            throw new InvalidOperationException("Unexpected end of stream.");
        }

        return buffer[0];
    }
}
