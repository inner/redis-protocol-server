namespace Redis.Tests;

public class StreamCommandTests
{
    [Fact(Timeout = 60_000)]
    public async Task XADD_WithAutoId_ReturnsGeneratedEntryId()
    {
        await using var cluster = await TestcontainersRedisCluster.StartAsync();
        var (host, port) = cluster.MasterEndpoint;
        var key = $"stream:auto:{Guid.NewGuid():N}";

        await using var client = await RedisRespClient.ConnectAsync(host, port);

        var response = await client.ExecuteCommandAsync(
            "XADD", key, "*", "temperature", "21");

        var entryId = ParseBulkString(response);
        Assert.Matches(@"^\d+-0$", entryId);
    }

    [Fact(Timeout = 60_000)]
    public async Task XADD_WithTimestampSequenceWildcard_IncrementsSequence()
    {
        await using var cluster = await TestcontainersRedisCluster.StartAsync();
        var (host, port) = cluster.MasterEndpoint;
        var key = $"stream:sequence:{Guid.NewGuid():N}";

        await using var client = await RedisRespClient.ConnectAsync(host, port);

        var firstResponse = await client.ExecuteCommandAsync(
            "XADD", key, "123-*", "temperature", "21");
        var secondResponse = await client.ExecuteCommandAsync(
            "XADD", key, "123-*", "temperature", "22");

        Assert.Equal("123-1", ParseBulkString(firstResponse));
        Assert.Equal("123-2", ParseBulkString(secondResponse));
    }

    [Fact(Timeout = 60_000)]
    public async Task XADD_ThenXRANGE_ReturnsEntriesInInsertionOrder()
    {
        await using var cluster = await TestcontainersRedisCluster.StartAsync();
        var (host, port) = cluster.MasterEndpoint;
        var key = $"stream:xrange:{Guid.NewGuid():N}";

        await using var client = await RedisRespClient.ConnectAsync(host, port);

        await client.ExecuteCommandAsync("XADD", key, "100-1", "temperature", "21");
        await client.ExecuteCommandAsync("XADD", key, "100-2", "temperature", "22");

        var response = await client.ExecuteCommandAsync("XRANGE", key, "-", "+");

        Assert.Equal(
            RespBuilder.InitArray(2) +
            RespBuilder.InitArray(2) +
            RespBuilder.BulkString("100-1") +
            RespBuilder.InitArray(2) +
            RespBuilder.BulkString("temperature") +
            RespBuilder.BulkString("21") +
            RespBuilder.InitArray(2) +
            RespBuilder.BulkString("100-2") +
            RespBuilder.InitArray(2) +
            RespBuilder.BulkString("temperature") +
            RespBuilder.BulkString("22"),
            response);
    }

    [Fact(Timeout = 60_000)]
    public async Task XREAD_BLOCK_FromZero_WhenEntryArrives_ReturnsNewEntry()
    {
        await using var cluster = await TestcontainersRedisCluster.StartAsync();
        var (host, port) = cluster.MasterEndpoint;
        var key = $"stream:xread:{Guid.NewGuid():N}";

        await using var consumer = await RedisRespClient.ConnectAsync(host, port);
        var blockedReadTask = consumer.ExecuteCommandAsync(
            "XREAD", "BLOCK", "0", "STREAMS", key, "0-0");

        await Task.Delay(200);

        await using var producer = await RedisRespClient.ConnectAsync(host, port);
        var addedResponse = await producer.ExecuteCommandAsync(
            "XADD", key, "500-1", "temperature", "23");
        Assert.Equal("500-1", ParseBulkString(addedResponse));

        var response = await blockedReadTask;

        Assert.Equal(
            RespBuilder.InitArray(1) +
            RespBuilder.InitArray(2) +
            RespBuilder.BulkString(key) +
            RespBuilder.InitArray(1) +
            RespBuilder.InitArray(2) +
            RespBuilder.BulkString("500-1") +
            RespBuilder.InitArray(2) +
            RespBuilder.BulkString("temperature") +
            RespBuilder.BulkString("23"),
            response);
    }

    private static string ParseBulkString(string response)
    {
        var separatorIndex = response.IndexOf("\r\n", StringComparison.Ordinal);
        var length = int.Parse(response[1..separatorIndex]);
        var valueStart = separatorIndex + 2;
        return response.Substring(valueStart, length);
    }
}
