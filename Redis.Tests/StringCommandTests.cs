using StackExchange.Redis;

namespace Redis.Tests;

public class StringCommandTests
{
    [Fact(Timeout = 60_000)]
    public async Task SET_ThenGET_ReturnsStoredValue()
    {
        await using var cluster = await TestcontainersRedisCluster.StartAsync();
        var (host, port) = cluster.MasterEndpoint;
        var key = $"string:set-get:{Guid.NewGuid():N}";
        var value = Guid.NewGuid().ToString("N");

        await using var connection = await StackExchangeRedisTestClient.ConnectAsync(
            host,
            port,
            () => cluster.BuildDiagnosticsAsync());
        var database = connection.GetDatabase();

        Assert.True(await database.StringSetAsync(key, value));
        Assert.Equal(value, await database.StringGetAsync(key));
    }

    [Fact(Timeout = 60_000)]
    public async Task SET_Twice_ThenGET_ReturnsLatestValue()
    {
        await using var cluster = await TestcontainersRedisCluster.StartAsync();
        var (host, port) = cluster.MasterEndpoint;
        var key = $"string:overwrite:{Guid.NewGuid():N}";

        await using var connection = await StackExchangeRedisTestClient.ConnectAsync(
            host,
            port,
            () => cluster.BuildDiagnosticsAsync());
        var database = connection.GetDatabase();

        Assert.True(await database.StringSetAsync(key, "first"));
        Assert.True(await database.StringSetAsync(key, "second"));
        Assert.Equal("second", await database.StringGetAsync(key));
    }

    [Fact(Timeout = 60_000)]
    public async Task SET_WithPX_ThenGET_ReturnsNullAfterExpiry()
    {
        await using var cluster = await TestcontainersRedisCluster.StartAsync();
        var (host, port) = cluster.MasterEndpoint;
        var key = $"string:px:{Guid.NewGuid():N}";

        await using var connection = await StackExchangeRedisTestClient.ConnectAsync(
            host,
            port,
            () => cluster.BuildDiagnosticsAsync());
        var database = connection.GetDatabase();

        var setResult = await database.ExecuteAsync("SET", key, "ephemeral", "PX", 150);

        Assert.Equal("OK", setResult.ToString());
        Assert.Equal("ephemeral", await database.StringGetAsync(key));

        await StackExchangeRedisTestClient.EventuallyAsync(
            async () => await database.StringGetAsync(key).ConfigureAwait(false) == RedisValue.Null,
            TimeSpan.FromSeconds(5),
            () => cluster.BuildDiagnosticsAsync());
    }
}
