namespace Redis.Tests;

public class BlockingListCommandTests
{
    [Fact(Timeout = 60_000)]
    public async Task BLPOP_WhenListHasItems_ReturnsKeyAndLeftmostValue()
    {
        await using var cluster = await TestcontainersRedisCluster.StartAsync();
        var (host, port) = cluster.MasterEndpoint;
        var key = $"blpop:ready:{Guid.NewGuid():N}";

        await using var producer = await StackExchangeRedisTestClient.ConnectAsync(
            host,
            port,
            () => cluster.BuildDiagnosticsAsync());
        var database = producer.GetDatabase();
        await database.ListLeftPushAsync(key, ["one", "two", "three"]);

        await using var consumer = await RedisRespClient.ConnectAsync(host, port);
        var response = await consumer.ExecuteCommandAsync("BLPOP", key, "1");

        Assert.Equal(
            RespBuilder.InitArray(2) +
            RespBuilder.BulkString(key) +
            RespBuilder.BulkString("three"),
            response);
    }

    [Fact(Timeout = 60_000)]
    public async Task BLPOP_WhenTimeoutExpires_ReturnsNullArray()
    {
        await using var cluster = await TestcontainersRedisCluster.StartAsync();
        var (host, port) = cluster.MasterEndpoint;
        var key = $"blpop:timeout:{Guid.NewGuid():N}";

        await using var consumer = await RedisRespClient.ConnectAsync(host, port);
        var response = await consumer.ExecuteCommandAsync("BLPOP", key, "0.1");

        Assert.Equal(RespBuilder.NullArray(), response);
    }

    [Fact(Timeout = 60_000)]
    public async Task BLPOP_WhenListIsEmpty_AndLPUSHPushesLater_ReturnsKeyAndValue()
    {
        await using var cluster = await TestcontainersRedisCluster.StartAsync();
        var (host, port) = cluster.MasterEndpoint;
        var key = $"blpop:lpush:{Guid.NewGuid():N}";

        await using var consumer = await RedisRespClient.ConnectAsync(host, port);
        var blockedPopTask = consumer.ExecuteCommandAsync("BLPOP", key, "0");

        await Task.Delay(200);

        await using var producer = await StackExchangeRedisTestClient.ConnectAsync(
            host,
            port,
            () => cluster.BuildDiagnosticsAsync());
        var database = producer.GetDatabase();
        await database.ListLeftPushAsync(key, "woken-by-lpush");

        var response = await blockedPopTask;
        Assert.Equal(
            RespBuilder.InitArray(2) +
            RespBuilder.BulkString(key) +
            RespBuilder.BulkString("woken-by-lpush"),
            response);
    }

    [Fact(Timeout = 60_000)]
    public async Task BLPOP_WhenListIsEmpty_AndRPUSHPushesLater_ReturnsKeyAndValue()
    {
        await using var cluster = await TestcontainersRedisCluster.StartAsync();
        var (host, port) = cluster.MasterEndpoint;
        var key = $"blpop:rpush:{Guid.NewGuid():N}";

        await using var consumer = await RedisRespClient.ConnectAsync(host, port);
        var blockedPopTask = consumer.ExecuteCommandAsync("BLPOP", key, "0");

        await Task.Delay(200);

        await using var producer = await StackExchangeRedisTestClient.ConnectAsync(
            host,
            port,
            () => cluster.BuildDiagnosticsAsync());
        var database = producer.GetDatabase();
        await database.ListRightPushAsync(key, "woken-by-rpush");

        var response = await blockedPopTask;
        Assert.Equal(
            RespBuilder.InitArray(2) +
            RespBuilder.BulkString(key) +
            RespBuilder.BulkString("woken-by-rpush"),
            response);
    }
}
