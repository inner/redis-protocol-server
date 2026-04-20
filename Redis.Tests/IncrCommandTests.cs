namespace Redis.Tests;

public class IncrCommandTests
{
    [Fact(Timeout = 60_000)]
    public async Task INCR_OnMissingKey_ReturnsOne()
    {
        await using var cluster = await TestcontainersRedisCluster.StartAsync();
        var (host, port) = cluster.MasterEndpoint;
        var key = $"incr:missing:{Guid.NewGuid():N}";

        await using var connection = await StackExchangeRedisTestClient.ConnectAsync(
            host,
            port,
            () => cluster.BuildDiagnosticsAsync());
        var database = connection.GetDatabase();

        Assert.Equal(1, await database.StringIncrementAsync(key));
        Assert.Equal("1", await database.StringGetAsync(key));
    }

    [Fact(Timeout = 60_000)]
    public async Task INCR_OnExistingInteger_IncrementsValue()
    {
        await using var cluster = await TestcontainersRedisCluster.StartAsync();
        var (host, port) = cluster.MasterEndpoint;
        var key = $"incr:existing:{Guid.NewGuid():N}";

        await using var connection = await StackExchangeRedisTestClient.ConnectAsync(
            host,
            port,
            () => cluster.BuildDiagnosticsAsync());
        var database = connection.GetDatabase();

        await database.StringSetAsync(key, "41");

        Assert.Equal(42, await database.StringIncrementAsync(key));
        Assert.Equal("42", await database.StringGetAsync(key));
    }

    [Fact(Timeout = 60_000)]
    public async Task INCR_OnNonInteger_ReturnsError()
    {
        await using var cluster = await TestcontainersRedisCluster.StartAsync();
        var (host, port) = cluster.MasterEndpoint;
        var key = $"incr:error:{Guid.NewGuid():N}";

        await using var client = await RedisRespClient.ConnectAsync(host, port);

        Assert.Equal(RespBuilder.SimpleString("OK"), await client.ExecuteCommandAsync("SET", key, "abc"));
        Assert.Equal(
            RespBuilder.Error("value is not an integer or out of range"),
            await client.ExecuteCommandAsync("INCR", key));
    }
}
