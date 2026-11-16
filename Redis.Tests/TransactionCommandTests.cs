namespace Redis.Tests;

public class TransactionCommandTests
{
    [DockerFact(Timeout = 60_000)]
    public async Task MULTI_EXEC_WithSETAndINCR_AppliesQueuedWritesInOrder()
    {
        await using var cluster = await TestcontainersRedisCluster.StartAsync();
        var (host, port) = cluster.MasterEndpoint;
        var key = $"transaction:exec:{Guid.NewGuid():N}";

        await using var client = await RedisRespClient.ConnectAsync(host, port);

        Assert.Equal(RespBuilder.SimpleString("OK"), await client.ExecuteCommandAsync("MULTI"));
        Assert.Equal(RespBuilder.SimpleString("QUEUED"), await client.ExecuteCommandAsync("SET", key, "1"));
        Assert.Equal(RespBuilder.SimpleString("QUEUED"), await client.ExecuteCommandAsync("INCR", key));
        Assert.Equal(
            RespBuilder.InitArray(2) +
            RespBuilder.SimpleString("OK") +
            RespBuilder.Integer(2),
            await client.ExecuteCommandAsync("EXEC"));

        await using var verifier = await StackExchangeRedisTestClient.ConnectAsync(
            host,
            port,
            () => cluster.BuildDiagnosticsAsync());
        Assert.Equal("2", await verifier.GetDatabase().StringGetAsync(key));
    }

    [DockerFact(Timeout = 60_000)]
    public async Task DISCARD_AfterMULTI_DoesNotApplyQueuedWrites()
    {
        await using var cluster = await TestcontainersRedisCluster.StartAsync();
        var (host, port) = cluster.MasterEndpoint;
        var key = $"transaction:discard:{Guid.NewGuid():N}";

        await using var client = await RedisRespClient.ConnectAsync(host, port);

        Assert.Equal(RespBuilder.SimpleString("OK"), await client.ExecuteCommandAsync("MULTI"));
        Assert.Equal(RespBuilder.SimpleString("QUEUED"), await client.ExecuteCommandAsync("SET", key, "discarded"));
        Assert.Equal(RespBuilder.SimpleString("OK"), await client.ExecuteCommandAsync("DISCARD"));
        Assert.Equal(RespBuilder.Null(), await client.ExecuteCommandAsync("GET", key));
    }

    [DockerFact(Timeout = 60_000)]
    public async Task EXEC_WithoutMULTI_ReturnsError()
    {
        await using var cluster = await TestcontainersRedisCluster.StartAsync();
        var (host, port) = cluster.MasterEndpoint;

        await using var client = await RedisRespClient.ConnectAsync(host, port);

        Assert.Equal(
            RespBuilder.Error("EXEC without MULTI"),
            await client.ExecuteCommandAsync("EXEC"));
    }

    [DockerFact(Timeout = 60_000)]
    public async Task MULTI_EXEC_ReplicatesTransactionalWritesToAllThreeReplicas()
    {
        await using var cluster = await TestcontainersRedisCluster.StartAsync(replicaCount: 3);
        var (host, port) = cluster.MasterEndpoint;
        var key = $"transaction:replication:{Guid.NewGuid():N}";

        await using var client = await RedisRespClient.ConnectAsync(host, port);

        Assert.Equal(RespBuilder.SimpleString("OK"), await client.ExecuteCommandAsync("MULTI"));
        Assert.Equal(RespBuilder.SimpleString("QUEUED"), await client.ExecuteCommandAsync("SET", key, "replicated"));
        Assert.Equal(
            RespBuilder.InitArray(1) +
            RespBuilder.SimpleString("OK"),
            await client.ExecuteCommandAsync("EXEC"));

        foreach (var replicaEndpoint in cluster.ReplicaEndpoints)
        {
            await StackExchangeRedisTestClient.EventuallyAsync(
                async () =>
                {
                    await using var replicaConnection = await StackExchangeRedisTestClient.ConnectAsync(
                        replicaEndpoint.Host,
                        replicaEndpoint.Port,
                        () => cluster.BuildDiagnosticsAsync());
                    return await replicaConnection.GetDatabase().StringGetAsync(key) == "replicated";
                },
                TimeSpan.FromSeconds(15),
                () => cluster.BuildDiagnosticsAsync());
        }
    }

    [DockerFact(Timeout = 60_000)]
    public async Task EXEC_WithLPUSH_RepliesOnlyWithTheExecArray()
    {
        await using var cluster = await TestcontainersRedisCluster.StartAsync();
        var (host, port) = cluster.MasterEndpoint;
        var key = $"transaction:lpush:{Guid.NewGuid():N}";

        await using var client = await RedisRespClient.ConnectAsync(host, port);

        Assert.Equal(RespBuilder.SimpleString("OK"), await client.ExecuteCommandAsync("MULTI"));
        Assert.Equal(RespBuilder.SimpleString("QUEUED"), await client.ExecuteCommandAsync("LPUSH", key, "a"));
        Assert.Equal(RespBuilder.SimpleString("QUEUED"), await client.ExecuteCommandAsync("LPOP", key));

        // Previously LPUSH and LPOP each sent their own reply before the
        // EXEC array, so the client read ":1" here.
        Assert.Equal(
            RespBuilder.InitArray(2) + RespBuilder.Integer(1) + RespBuilder.BulkString("a"),
            await client.ExecuteCommandAsync("EXEC"));
        Assert.Equal(RespBuilder.SimpleString("PONG"), await client.ExecuteCommandAsync("PING"));
    }

    [DockerFact(Timeout = 60_000)]
    public async Task EXEC_OnMaster_IsAppliedOnceOnReplica()
    {
        await using var cluster = await TestcontainersRedisCluster.StartAsync();
        var (host, port) = cluster.MasterEndpoint;
        var (replicaHost, replicaPort) = cluster.ReplicaEndpoints[0];
        var key = $"transaction:replicated:{Guid.NewGuid():N}";

        await using var client = await RedisRespClient.ConnectAsync(host, port);
        await client.ExecuteCommandAsync("MULTI");
        await client.ExecuteCommandAsync("INCR", key);
        await client.ExecuteCommandAsync("INCR", key);
        await client.ExecuteCommandAsync("EXEC");

        // Previously the replica got MULTI, both queued commands, both again
        // when EXEC ran them, and EXEC: it ended at 4 instead of 2.
        await using var replica = await RedisRespClient.ConnectAsync(replicaHost, replicaPort);
        await StackExchangeRedisTestClient.EventuallyAsync(
            async () => await replica.ExecuteCommandAsync("GET", key) == RespBuilder.BulkString("2"),
            TimeSpan.FromSeconds(5),
            () => cluster.BuildDiagnosticsAsync());

        await Task.Delay(500);
        Assert.Equal(RespBuilder.BulkString("2"), await replica.ExecuteCommandAsync("GET", key));
    }

    [DockerFact(Timeout = 60_000)]
    public async Task MULTI_Nested_ReturnsError()
    {
        await using var cluster = await TestcontainersRedisCluster.StartAsync();
        var (host, port) = cluster.MasterEndpoint;

        await using var client = await RedisRespClient.ConnectAsync(host, port);

        Assert.Equal(RespBuilder.SimpleString("OK"), await client.ExecuteCommandAsync("MULTI"));
        Assert.Equal(
            RespBuilder.Error("MULTI calls can not be nested"),
            await client.ExecuteCommandAsync("MULTI"));
    }
}
