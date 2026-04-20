namespace Redis.Tests;

public class SortedSetCommandTests
{
    [Fact(Timeout = 60_000)]
    public async Task ZADD_ThenZRANGE_ReturnsMembersOrderedByScore()
    {
        await using var cluster = await TestcontainersRedisCluster.StartAsync();
        var (host, port) = cluster.MasterEndpoint;
        var key = $"zset:zrange:{Guid.NewGuid():N}";

        await using var connection = await StackExchangeRedisTestClient.ConnectAsync(
            host,
            port,
            () => cluster.BuildDiagnosticsAsync());
        var database = connection.GetDatabase();

        Assert.True(await database.SortedSetAddAsync(key, "bravo", 2));
        Assert.True(await database.SortedSetAddAsync(key, "alpha", 1));
        Assert.True(await database.SortedSetAddAsync(key, "charlie", 3));

        Assert.Equal(
            ["alpha", "bravo", "charlie"],
            (await database.SortedSetRangeByRankAsync(key, 0, -1)).Select(value => value.ToString()).ToArray());
    }

    [Fact(Timeout = 60_000)]
    public async Task ZADD_ThenZSCORE_ReturnsStoredScore()
    {
        await using var cluster = await TestcontainersRedisCluster.StartAsync();
        var (host, port) = cluster.MasterEndpoint;
        var key = $"zset:zscore:{Guid.NewGuid():N}";

        await using var connection = await StackExchangeRedisTestClient.ConnectAsync(
            host,
            port,
            () => cluster.BuildDiagnosticsAsync());
        var database = connection.GetDatabase();

        Assert.True(await database.SortedSetAddAsync(key, "alpha", 1.5));
        Assert.Equal(1.5, await database.SortedSetScoreAsync(key, "alpha"));
    }

    [Fact(Timeout = 60_000)]
    public async Task ZADD_ThenZRANK_ReturnsMemberRank()
    {
        await using var cluster = await TestcontainersRedisCluster.StartAsync();
        var (host, port) = cluster.MasterEndpoint;
        var key = $"zset:zrank:{Guid.NewGuid():N}";

        await using var connection = await StackExchangeRedisTestClient.ConnectAsync(
            host,
            port,
            () => cluster.BuildDiagnosticsAsync());
        var database = connection.GetDatabase();

        await database.SortedSetAddAsync(key, "bravo", 2);
        await database.SortedSetAddAsync(key, "alpha", 1);
        await database.SortedSetAddAsync(key, "charlie", 3);

        Assert.Equal(1, await database.SortedSetRankAsync(key, "bravo"));
    }

    [Fact(Timeout = 60_000)]
    public async Task ZADD_ThenZCARD_ReturnsSortedSetLength()
    {
        await using var cluster = await TestcontainersRedisCluster.StartAsync();
        var (host, port) = cluster.MasterEndpoint;
        var key = $"zset:zcard:{Guid.NewGuid():N}";

        await using var connection = await StackExchangeRedisTestClient.ConnectAsync(
            host,
            port,
            () => cluster.BuildDiagnosticsAsync());
        var database = connection.GetDatabase();

        await database.SortedSetAddAsync(key, "alpha", 1);
        await database.SortedSetAddAsync(key, "bravo", 2);
        await database.SortedSetAddAsync(key, "charlie", 3);

        Assert.Equal(3, await database.SortedSetLengthAsync(key));
    }

    [Fact(Timeout = 60_000)]
    public async Task ZREM_RemovesMemberAndUpdatesSortedSetState()
    {
        await using var cluster = await TestcontainersRedisCluster.StartAsync();
        var (host, port) = cluster.MasterEndpoint;
        var key = $"zset:zrem:{Guid.NewGuid():N}";

        await using var connection = await StackExchangeRedisTestClient.ConnectAsync(
            host,
            port,
            () => cluster.BuildDiagnosticsAsync());
        var database = connection.GetDatabase();

        await database.SortedSetAddAsync(key, "alpha", 1);
        await database.SortedSetAddAsync(key, "bravo", 2);
        await database.SortedSetAddAsync(key, "charlie", 3);

        Assert.True(await database.SortedSetRemoveAsync(key, "bravo"));
        Assert.Null(await database.SortedSetRankAsync(key, "bravo"));
        Assert.Equal(2, await database.SortedSetLengthAsync(key));
        Assert.Equal(
            ["alpha", "charlie"],
            (await database.SortedSetRangeByRankAsync(key, 0, -1)).Select(value => value.ToString()).ToArray());
    }

    [Fact(Timeout = 60_000)]
    public async Task ZADD_ReplicatesSortedSetWriteToAllThreeReplicas()
    {
        await using var cluster = await TestcontainersRedisCluster.StartAsync(replicaCount: 3);
        var (host, port) = cluster.MasterEndpoint;
        var key = $"zset:replication:{Guid.NewGuid():N}";

        await using var connection = await StackExchangeRedisTestClient.ConnectAsync(
            host,
            port,
            () => cluster.BuildDiagnosticsAsync());
        var database = connection.GetDatabase();

        Assert.True(await database.SortedSetAddAsync(key, "alpha", 1));
        Assert.True(await database.SortedSetAddAsync(key, "bravo", 2));

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
                    var members = await replicaDatabase.SortedSetRangeByRankAsync(key, 0, -1);
                    return members.Select(value => value.ToString()).SequenceEqual(["alpha", "bravo"]);
                },
                TimeSpan.FromSeconds(15),
                () => cluster.BuildDiagnosticsAsync());
        }
    }
}
