using System.Net.Sockets;
using System.Text;

namespace Redis.Tests;

// Wire-level behavior: framing, binary safety and connection handling.
public class ProtocolTests
{
    [DockerFact(Timeout = 60_000)]
    public async Task PipelinedCommands_InOneWrite_EachGetAReplyInOrder()
    {
        await using var cluster = await TestcontainersRedisCluster.StartAsync();
        var (host, port) = cluster.MasterEndpoint;
        var key = $"protocol:pipeline:{Guid.NewGuid():N}";

        await using var client = await RedisRespClient.ConnectAsync(host, port);

        // The value looks like an array header, which the old regex-based
        // splitter mistook for the start of a new command.
        await client.SendRawAsync(
            RespBuilder.ArrayFromCommands("SET", key, "*2\r\n") +
            RespBuilder.ArrayFromCommands("GET", key) +
            RespBuilder.ArrayFromCommands("PING"));

        Assert.Equal(RespBuilder.SimpleString("OK"), await client.ReadResponseAsync());
        Assert.Equal(RespBuilder.BulkString("*2\r\n"), await client.ReadResponseAsync());
        Assert.Equal(RespBuilder.SimpleString("PONG"), await client.ReadResponseAsync());
    }

    [DockerFact(Timeout = 60_000)]
    public async Task CommandSplitAcrossWrites_IsExecutedOnceComplete()
    {
        await using var cluster = await TestcontainersRedisCluster.StartAsync();
        var (host, port) = cluster.MasterEndpoint;

        await using var client = await RedisRespClient.ConnectAsync(host, port);
        var echo = RespBuilder.ArrayFromCommands("ECHO", "fragmented");

        await client.SendRawAsync(echo[..7]);
        await Task.Delay(200);
        await client.SendRawAsync(echo[7..]);

        Assert.Equal(RespBuilder.BulkString("fragmented"), await client.ReadResponseAsync());
    }

    [DockerFact(Timeout = 60_000)]
    public async Task InlineCommand_IsExecuted()
    {
        await using var cluster = await TestcontainersRedisCluster.StartAsync();
        var (host, port) = cluster.MasterEndpoint;

        await using var client = await RedisRespClient.ConnectAsync(host, port);
        await client.SendRawAsync("ECHO inline\r\n");

        Assert.Equal(RespBuilder.BulkString("inline"), await client.ReadResponseAsync());
    }

    [DockerFact(Timeout = 60_000)]
    public async Task BitmapWithHighBytes_RoundTripsByteExact()
    {
        await using var cluster = await TestcontainersRedisCluster.StartAsync();
        var (host, port) = cluster.MasterEndpoint;
        var key = $"protocol:binary:{Guid.NewGuid():N}";

        await using var client = await RedisRespClient.ConnectAsync(host, port);

        // Bit 0 is the most significant bit of byte 0, so this stores 0xFF.
        for (var bit = 0; bit < 8; bit++)
        {
            await client.ExecuteCommandAsync("SETBIT", key, bit.ToString(), "1");
        }

        Assert.Equal(RespBuilder.BulkString("ÿ"), await client.ExecuteCommandAsync("GET", key));
        Assert.Equal(RespBuilder.Integer(1), await client.ExecuteCommandAsync("STRLEN", key));
    }

    [DockerFact(Timeout = 60_000)]
    public async Task ClientHalfClose_DoesNotMakeServerSpin()
    {
        await using var cluster = await TestcontainersRedisCluster.StartAsync();
        var (host, port) = cluster.MasterEndpoint;

        using var tcpClient = new TcpClient();
        await tcpClient.ConnectAsync(host, port);
        var stream = tcpClient.GetStream();

        await stream.WriteAsync(Encoding.Latin1.GetBytes(RespBuilder.ArrayFromCommands("PING")));
        tcpClient.Client.Shutdown(SocketShutdown.Send);

        // Before the fix the server answered end-of-stream with an endless
        // series of "$-1\r\n". Now it sends the PONG and closes.
        using var reader = new MemoryStream();
        await stream.CopyToAsync(reader);

        Assert.Equal(RespBuilder.SimpleString("PONG"), Encoding.Latin1.GetString(reader.ToArray()));
    }

    [DockerFact(Timeout = 60_000)]
    public async Task PSETEX_StoresValueWithMillisecondTtl()
    {
        await using var cluster = await TestcontainersRedisCluster.StartAsync();
        var (host, port) = cluster.MasterEndpoint;
        var key = $"protocol:psetex:{Guid.NewGuid():N}";

        await using var connection = await StackExchangeRedisTestClient.ConnectAsync(
            host,
            port,
            () => cluster.BuildDiagnosticsAsync());
        var database = connection.GetDatabase();

        // StackExchange.Redis sends PSETEX for a timed StringSet.
        Assert.True(await database.StringSetAsync(key, "short-lived", TimeSpan.FromMilliseconds(200)));
        Assert.Equal("short-lived", await database.StringGetAsync(key));

        await Task.Delay(400);
        Assert.True((await database.StringGetAsync(key)).IsNull);
    }
}
