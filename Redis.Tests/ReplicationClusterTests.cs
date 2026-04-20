namespace Redis.Tests;

public class ReplicationClusterTests
{
    [Fact(Timeout = 60_000)]
    public async Task SET_ThenGET_ReplicatesValueToAllThreeReplicas()
    {
        await using var cluster = await TestcontainersRedisCluster.StartAsync(replicaCount: 3);
        var (masterHost, masterPort) = cluster.MasterEndpoint;
        var probeKey = $"replication:probe:{Guid.NewGuid():N}";
        var probeValue = Guid.NewGuid().ToString("N");

        Assert.Equal(3, cluster.Replicas.Count);

        await using var masterConnection = await StackExchangeRedisTestClient.ConnectAsync(
            masterHost,
            masterPort,
            () => cluster.BuildDiagnosticsAsync());
        var masterDatabase = masterConnection.GetDatabase();

        Assert.True(await masterDatabase.StringSetAsync(probeKey, probeValue));
        Assert.Equal(probeValue, await masterDatabase.StringGetAsync(probeKey));

        await AssertReplicatedToAllReplicasAsync(cluster, probeKey, probeValue);
    }

    [Fact(Timeout = 60_000)]
    public async Task SET_MultipleKeys_ReplicatesAllWritesToAllThreeReplicas()
    {
        await using var cluster = await TestcontainersRedisCluster.StartAsync(replicaCount: 3);
        var (masterHost, masterPort) = cluster.MasterEndpoint;
        var writes = Enumerable.Range(1, 3)
            .Select(index => (
                Key: $"replication:sequence:{index}:{Guid.NewGuid():N}",
                Value: $"value-{index}-{Guid.NewGuid():N}"))
            .ToArray();

        await using var masterConnection = await StackExchangeRedisTestClient.ConnectAsync(
            masterHost,
            masterPort,
            () => cluster.BuildDiagnosticsAsync());
        var masterDatabase = masterConnection.GetDatabase();

        foreach (var write in writes)
        {
            Assert.True(await masterDatabase.StringSetAsync(write.Key, write.Value));
            Assert.Equal(write.Value, await masterDatabase.StringGetAsync(write.Key));
        }

        foreach (var write in writes)
        {
            await AssertReplicatedToAllReplicasAsync(cluster, write.Key, write.Value);
        }
    }

    [Fact(Timeout = 60_000)]
    public async Task WAIT_WithThreeReplicas_ReturnsThreeAfterSET()
    {
        await using var cluster = await TestcontainersRedisCluster.StartAsync(replicaCount: 3);
        var (masterHost, masterPort) = cluster.MasterEndpoint;
        var probeKey = $"replication:wait:{Guid.NewGuid():N}";
        var probeValue = Guid.NewGuid().ToString("N");

        await using var masterConnection = await StackExchangeRedisTestClient.ConnectAsync(
            masterHost,
            masterPort,
            () => cluster.BuildDiagnosticsAsync());
        var masterDatabase = masterConnection.GetDatabase();

        Assert.True(await masterDatabase.StringSetAsync(probeKey, probeValue));

        var waitResult = (int)await masterDatabase.ExecuteAsync("WAIT", 3, 5_000);
        Assert.Equal(3, waitResult);

        await AssertReplicatedToAllReplicasAsync(cluster, probeKey, probeValue);
    }

    [Fact(Timeout = 60_000)]
    public async Task RPUSH_ReplicatesListWriteToAllThreeReplicas()
    {
        await using var cluster = await TestcontainersRedisCluster.StartAsync(replicaCount: 3);
        var (masterHost, masterPort) = cluster.MasterEndpoint;
        var key = $"replication:rpush:{Guid.NewGuid():N}";

        await using var masterConnection = await StackExchangeRedisTestClient.ConnectAsync(
            masterHost,
            masterPort,
            () => cluster.BuildDiagnosticsAsync());
        var masterDatabase = masterConnection.GetDatabase();

        Assert.Equal(3, await masterDatabase.ListRightPushAsync(key, ["one", "two", "three"]));

        foreach (var replicaEndpoint in cluster.ReplicaEndpoints)
        {
            await StackExchangeRedisTestClient.EventuallyAsync(
                async () =>
                {
                    await using var replicaConnection = await StackExchangeRedisTestClient.ConnectAsync(
                        replicaEndpoint.Host,
                        replicaEndpoint.Port,
                        () => cluster.BuildDiagnosticsAsync());
                    var values = await replicaConnection.GetDatabase().ListRangeAsync(key, 0, -1);
                    return values.Select(value => value.ToString()).SequenceEqual(["one", "two", "three"]);
                },
                TimeSpan.FromSeconds(15),
                () => cluster.BuildDiagnosticsAsync());
        }
    }

    [Fact(Timeout = 60_000)]
    public async Task LPOP_ReplicatesListMutationToAllThreeReplicas()
    {
        await using var cluster = await TestcontainersRedisCluster.StartAsync(replicaCount: 3);
        var (masterHost, masterPort) = cluster.MasterEndpoint;
        var key = $"replication:lpop:{Guid.NewGuid():N}";

        await using var masterConnection = await StackExchangeRedisTestClient.ConnectAsync(
            masterHost,
            masterPort,
            () => cluster.BuildDiagnosticsAsync());
        var masterDatabase = masterConnection.GetDatabase();

        await masterDatabase.ListRightPushAsync(key, ["one", "two", "three"]);
        Assert.Equal("one", await masterDatabase.ListLeftPopAsync(key));

        foreach (var replicaEndpoint in cluster.ReplicaEndpoints)
        {
            await StackExchangeRedisTestClient.EventuallyAsync(
                async () =>
                {
                    await using var replicaConnection = await StackExchangeRedisTestClient.ConnectAsync(
                        replicaEndpoint.Host,
                        replicaEndpoint.Port,
                        () => cluster.BuildDiagnosticsAsync());
                    var values = await replicaConnection.GetDatabase().ListRangeAsync(key, 0, -1);
                    return values.Select(value => value.ToString()).SequenceEqual(["two", "three"]);
                },
                TimeSpan.FromSeconds(15),
                () => cluster.BuildDiagnosticsAsync());
        }
    }

    [Fact(Timeout = 60_000)]
    public async Task ZREM_ReplicatesSortedSetRemovalToAllThreeReplicas()
    {
        await using var cluster = await TestcontainersRedisCluster.StartAsync(replicaCount: 3);
        var (masterHost, masterPort) = cluster.MasterEndpoint;
        var key = $"replication:zrem:{Guid.NewGuid():N}";

        await using var masterConnection = await StackExchangeRedisTestClient.ConnectAsync(
            masterHost,
            masterPort,
            () => cluster.BuildDiagnosticsAsync());
        var masterDatabase = masterConnection.GetDatabase();

        await masterDatabase.SortedSetAddAsync(key, "alpha", 1);
        await masterDatabase.SortedSetAddAsync(key, "bravo", 2);
        Assert.True(await masterDatabase.SortedSetRemoveAsync(key, "alpha"));

        foreach (var replicaEndpoint in cluster.ReplicaEndpoints)
        {
            await StackExchangeRedisTestClient.EventuallyAsync(
                async () =>
                {
                    await using var replicaConnection = await StackExchangeRedisTestClient.ConnectAsync(
                        replicaEndpoint.Host,
                        replicaEndpoint.Port,
                        () => cluster.BuildDiagnosticsAsync());
                    var members = await replicaConnection.GetDatabase().SortedSetRangeByRankAsync(key, 0, -1);
                    return members.Select(value => value.ToString()).SequenceEqual(["bravo"]);
                },
                TimeSpan.FromSeconds(15),
                () => cluster.BuildDiagnosticsAsync());
        }
    }

    [Fact(Timeout = 60_000)]
    public async Task DEL_ReplicatesDeletionToAllThreeReplicas()
    {
        await using var cluster = await TestcontainersRedisCluster.StartAsync(replicaCount: 3);
        var (masterHost, masterPort) = cluster.MasterEndpoint;
        var key = $"replication:del:{Guid.NewGuid():N}";

        await using var masterConnection = await StackExchangeRedisTestClient.ConnectAsync(
            masterHost,
            masterPort,
            () => cluster.BuildDiagnosticsAsync());
        var masterDatabase = masterConnection.GetDatabase();

        Assert.True(await masterDatabase.StringSetAsync(key, "value"));
        Assert.True(await masterDatabase.KeyDeleteAsync(key));

        foreach (var replicaEndpoint in cluster.ReplicaEndpoints)
        {
            await StackExchangeRedisTestClient.EventuallyAsync(
                async () =>
                {
                    await using var replicaConnection = await StackExchangeRedisTestClient.ConnectAsync(
                        replicaEndpoint.Host,
                        replicaEndpoint.Port,
                        () => cluster.BuildDiagnosticsAsync());
                    return !await replicaConnection.GetDatabase().KeyExistsAsync(key);
                },
                TimeSpan.FromSeconds(15),
                () => cluster.BuildDiagnosticsAsync());
        }
    }

    private static async Task AssertReplicatedToAllReplicasAsync(
        TestcontainersRedisCluster cluster,
        string key,
        string expectedValue)
    {
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
                    return await replicaDatabase.StringGetAsync(key) == expectedValue;
                },
                timeout: TimeSpan.FromSeconds(15),
                diagnosticsFactory: () => cluster.BuildDiagnosticsAsync());
        }
    }
}
