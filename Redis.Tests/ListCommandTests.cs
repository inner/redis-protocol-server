namespace Redis.Tests;

public class ListCommandTests
{
    [Fact(Timeout = 60_000)]
    public async Task LPUSH_ThenLRANGE_ReturnsValuesInExpectedOrder()
    {
        await using var cluster = await TestcontainersRedisCluster.StartAsync();
        var (host, port) = cluster.MasterEndpoint;
        var key = $"list:lpush:{Guid.NewGuid():N}";

        await using var connection = await StackExchangeRedisTestClient.ConnectAsync(
            host,
            port,
            () => cluster.BuildDiagnosticsAsync());
        var database = connection.GetDatabase();

        var pushed = await database.ListLeftPushAsync(key, ["one", "two", "three"]);

        Assert.Equal(3, pushed);
        Assert.Equal(
            ["three", "two", "one"],
            (await database.ListRangeAsync(key, 0, -1)).Select(value => value.ToString()).ToArray());
    }

    [Fact(Timeout = 60_000)]
    public async Task RPUSH_ThenLRANGE_ReturnsValuesInExpectedOrder()
    {
        await using var cluster = await TestcontainersRedisCluster.StartAsync();
        var (host, port) = cluster.MasterEndpoint;
        var key = $"list:rpush:{Guid.NewGuid():N}";

        await using var connection = await StackExchangeRedisTestClient.ConnectAsync(
            host,
            port,
            () => cluster.BuildDiagnosticsAsync());
        var database = connection.GetDatabase();

        var pushed = await database.ListRightPushAsync(key, ["one", "two", "three"]);

        Assert.Equal(3, pushed);
        Assert.Equal(
            ["one", "two", "three"],
            (await database.ListRangeAsync(key, 0, -1)).Select(value => value.ToString()).ToArray());
    }

    [Fact(Timeout = 60_000)]
    public async Task LPUSH_ThenLLEN_ReturnsListLength()
    {
        await using var cluster = await TestcontainersRedisCluster.StartAsync();
        var (host, port) = cluster.MasterEndpoint;
        var key = $"list:llen:{Guid.NewGuid():N}";

        await using var connection = await StackExchangeRedisTestClient.ConnectAsync(
            host,
            port,
            () => cluster.BuildDiagnosticsAsync());
        var database = connection.GetDatabase();

        await database.ListLeftPushAsync(key, ["one", "two", "three"]);

        Assert.Equal(3, await database.ListLengthAsync(key));
    }

    [Fact(Timeout = 60_000)]
    public async Task LPUSH_ThenLPOP_ReturnsLeftmostValue()
    {
        await using var cluster = await TestcontainersRedisCluster.StartAsync();
        var (host, port) = cluster.MasterEndpoint;
        var key = $"list:lpop:{Guid.NewGuid():N}";

        await using var connection = await StackExchangeRedisTestClient.ConnectAsync(
            host,
            port,
            () => cluster.BuildDiagnosticsAsync());
        var database = connection.GetDatabase();

        await database.ListLeftPushAsync(key, ["one", "two", "three"]);

        Assert.Equal("three", await database.ListLeftPopAsync(key));
        Assert.Equal(
            ["two", "one"],
            (await database.ListRangeAsync(key, 0, -1)).Select(value => value.ToString()).ToArray());
    }

    [Fact(Timeout = 60_000)]
    public async Task LPUSH_ReplicatesListWriteToAllThreeReplicas()
    {
        await using var cluster = await TestcontainersRedisCluster.StartAsync(replicaCount: 3);
        var (host, port) = cluster.MasterEndpoint;
        var key = $"list:replication:{Guid.NewGuid():N}";

        await using var connection = await StackExchangeRedisTestClient.ConnectAsync(
            host,
            port,
            () => cluster.BuildDiagnosticsAsync());
        var database = connection.GetDatabase();

        var pushed = await database.ListLeftPushAsync(key, ["one", "two", "three"]);
        Assert.Equal(3, pushed);

        foreach (var replicaEndpoint in cluster.ReplicaEndpoints)
        {
            await StackExchangeRedisTestClient.EventuallyAsync(
                async () =>
                {
                    await using var replicaConnection = await StackExchangeRedisTestClient.ConnectAsync(
                        replicaEndpoint.Host,
                        replicaEndpoint.Port,
                        () => cluster.BuildDiagnosticsAsync());
                    var replicaDatabase = replicaConnection.GetDatabase();
                    var values = await replicaDatabase.ListRangeAsync(key, 0, -1);
                    return values.Select(value => value.ToString())
                        .SequenceEqual(["three", "two", "one"]);
                },
                TimeSpan.FromSeconds(15),
                () => cluster.BuildDiagnosticsAsync());
        }
    }
}
