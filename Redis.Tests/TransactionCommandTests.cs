namespace Redis.Tests;

public class TransactionCommandTests
{
    [Fact(Timeout = 60_000)]
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

    [Fact(Timeout = 60_000)]
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

    [Fact(Timeout = 60_000)]
    public async Task EXEC_WithoutMULTI_ReturnsError()
    {
        await using var cluster = await TestcontainersRedisCluster.StartAsync();
        var (host, port) = cluster.MasterEndpoint;

        await using var client = await RedisRespClient.ConnectAsync(host, port);

        Assert.Equal(
            RespBuilder.Error("EXEC without MULTI"),
            await client.ExecuteCommandAsync("EXEC"));
    }

    [Fact(Timeout = 60_000)]
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
}
