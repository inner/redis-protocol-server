namespace Redis.Tests;

public class KeyCommandTests
{
    [Fact(Timeout = 60_000)]
    public async Task SET_ThenEXISTS_ReturnsOne()
    {
        await using var cluster = await TestcontainersRedisCluster.StartAsync();
        var (host, port) = cluster.MasterEndpoint;
        var key = $"keys:exists:{Guid.NewGuid():N}";

        await using var connection = await StackExchangeRedisTestClient.ConnectAsync(
            host,
            port,
            () => cluster.BuildDiagnosticsAsync());
        var database = connection.GetDatabase();

        await database.StringSetAsync(key, "value");

        Assert.True(await database.KeyExistsAsync(key));
    }

    [Fact(Timeout = 60_000)]
    public async Task DEL_RemovesKeyAndReturnsDeletedCount()
    {
        await using var cluster = await TestcontainersRedisCluster.StartAsync();
        var (host, port) = cluster.MasterEndpoint;
        var key = $"keys:del:{Guid.NewGuid():N}";

        await using var connection = await StackExchangeRedisTestClient.ConnectAsync(
            host,
            port,
            () => cluster.BuildDiagnosticsAsync());
        var database = connection.GetDatabase();

        await database.StringSetAsync(key, "value");

        Assert.True(await database.KeyDeleteAsync(key));
        Assert.False(await database.KeyExistsAsync(key));
    }

    [Fact(Timeout = 60_000)]
    public async Task TYPE_AfterSET_ReturnsString()
    {
        await using var cluster = await TestcontainersRedisCluster.StartAsync();
        var (host, port) = cluster.MasterEndpoint;
        var key = $"type:string:{Guid.NewGuid():N}";

        await using var client = await RedisRespClient.ConnectAsync(host, port);

        Assert.Equal(RespBuilder.SimpleString("OK"), await client.ExecuteCommandAsync("SET", key, "value"));
        Assert.Equal(RespBuilder.SimpleString("string"), await client.ExecuteCommandAsync("TYPE", key));
    }

    [Fact(Timeout = 60_000)]
    public async Task TYPE_AfterLPUSH_ReturnsList()
    {
        await using var cluster = await TestcontainersRedisCluster.StartAsync();
        var (host, port) = cluster.MasterEndpoint;
        var key = $"type:list:{Guid.NewGuid():N}";

        await using var client = await RedisRespClient.ConnectAsync(host, port);

        Assert.Equal(RespBuilder.Integer(2), await client.ExecuteCommandAsync("LPUSH", key, "a", "b"));
        Assert.Equal(RespBuilder.SimpleString("list"), await client.ExecuteCommandAsync("TYPE", key));
    }

    [Fact(Timeout = 60_000)]
    public async Task TYPE_AfterXADD_ReturnsStream()
    {
        await using var cluster = await TestcontainersRedisCluster.StartAsync();
        var (host, port) = cluster.MasterEndpoint;
        var key = $"type:stream:{Guid.NewGuid():N}";

        await using var client = await RedisRespClient.ConnectAsync(host, port);

        await client.ExecuteCommandAsync("XADD", key, "100-1", "temperature", "21");
        Assert.Equal(RespBuilder.SimpleString("stream"), await client.ExecuteCommandAsync("TYPE", key));
    }

    [Fact(Timeout = 60_000)]
    public async Task TYPE_AfterZADD_ReturnsZset()
    {
        await using var cluster = await TestcontainersRedisCluster.StartAsync();
        var (host, port) = cluster.MasterEndpoint;
        var key = $"type:zset:{Guid.NewGuid():N}";

        await using var client = await RedisRespClient.ConnectAsync(host, port);

        Assert.Equal(RespBuilder.Integer(1), await client.ExecuteCommandAsync("ZADD", key, "1", "alpha"));
        Assert.Equal(RespBuilder.SimpleString("zset"), await client.ExecuteCommandAsync("TYPE", key));
    }

    [Fact(Timeout = 60_000)]
    public async Task TYPE_OnMissingKey_ReturnsNone()
    {
        await using var cluster = await TestcontainersRedisCluster.StartAsync();
        var (host, port) = cluster.MasterEndpoint;
        var key = $"type:none:{Guid.NewGuid():N}";

        await using var client = await RedisRespClient.ConnectAsync(host, port);

        Assert.Equal(RespBuilder.SimpleString("none"), await client.ExecuteCommandAsync("TYPE", key));
    }

    [Fact(Timeout = 60_000)]
    public async Task KEYS_WithWildcard_ReturnsMatchingKeys()
    {
        await using var cluster = await TestcontainersRedisCluster.StartAsync();
        var (host, port) = cluster.MasterEndpoint;
        var prefix = $"keys:pattern:{Guid.NewGuid():N}";
        var matchingKeys = new[]
        {
            $"{prefix}:one",
            $"{prefix}:two"
        };
        var nonMatchingKey = $"keys:other:{Guid.NewGuid():N}";

        await using var client = await RedisRespClient.ConnectAsync(host, port);

        Assert.Equal(RespBuilder.SimpleString("OK"), await client.ExecuteCommandAsync("SET", matchingKeys[0], "one"));
        Assert.Equal(RespBuilder.SimpleString("OK"), await client.ExecuteCommandAsync("SET", matchingKeys[1], "two"));
        Assert.Equal(RespBuilder.SimpleString("OK"), await client.ExecuteCommandAsync("SET", nonMatchingKey, "other"));

        var response = await client.ExecuteCommandAsync("KEYS", $"{prefix}*");
        var keys = ParseBulkStringArray(response);

        Assert.Equal(matchingKeys.OrderBy(x => x), keys.OrderBy(x => x));
    }

    private static string[] ParseBulkStringArray(string response)
    {
        if (response == RespBuilder.EmptyArray())
        {
            return [];
        }

        var parts = response.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        return parts
            .Where((part, index) => index > 0 && !part.StartsWith('*') && !part.StartsWith('$'))
            .ToArray();
    }
}
