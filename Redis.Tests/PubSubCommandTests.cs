namespace Redis.Tests;

public class PubSubCommandTests
{
    [Fact(Timeout = 60_000)]
    public async Task SUBSCRIBE_ThenPUBLISH_DeliversMessageToSubscriber()
    {
        await using var cluster = await TestcontainersRedisCluster.StartAsync();
        var (host, port) = cluster.MasterEndpoint;
        var channel = $"pubsub:channel:{Guid.NewGuid():N}";

        await using var subscriber = await RedisRespClient.ConnectAsync(host, port);
        await using var publisher = await RedisRespClient.ConnectAsync(host, port);

        Assert.Equal(
            RespBuilder.InitArray(3) +
            RespBuilder.BulkString("subscribe") +
            RespBuilder.BulkString(channel) +
            RespBuilder.Integer(1),
            await subscriber.ExecuteCommandAsync("SUBSCRIBE", channel));

        Assert.Equal(RespBuilder.Integer(1), await publisher.ExecuteCommandAsync("PUBLISH", channel, "hello"));
        Assert.Equal(
            RespBuilder.InitArray(3) +
            RespBuilder.BulkString("message") +
            RespBuilder.BulkString(channel) +
            RespBuilder.BulkString("hello"),
            await subscriber.ReadResponseAsync());
    }

    [Fact(Timeout = 60_000)]
    public async Task PUBLISH_ReturnsSubscriberCount()
    {
        await using var cluster = await TestcontainersRedisCluster.StartAsync();
        var (host, port) = cluster.MasterEndpoint;
        var channel = $"pubsub:count:{Guid.NewGuid():N}";

        await using var subscriberOne = await RedisRespClient.ConnectAsync(host, port);
        await using var subscriberTwo = await RedisRespClient.ConnectAsync(host, port);
        await using var publisher = await RedisRespClient.ConnectAsync(host, port);

        await subscriberOne.ExecuteCommandAsync("SUBSCRIBE", channel);
        await subscriberTwo.ExecuteCommandAsync("SUBSCRIBE", channel);

        Assert.Equal(RespBuilder.Integer(2), await publisher.ExecuteCommandAsync("PUBLISH", channel, "hello"));

        Assert.Equal(
            RespBuilder.InitArray(3) +
            RespBuilder.BulkString("message") +
            RespBuilder.BulkString(channel) +
            RespBuilder.BulkString("hello"),
            await subscriberOne.ReadResponseAsync());
        Assert.Equal(
            RespBuilder.InitArray(3) +
            RespBuilder.BulkString("message") +
            RespBuilder.BulkString(channel) +
            RespBuilder.BulkString("hello"),
            await subscriberTwo.ReadResponseAsync());
    }

    [Fact(Timeout = 60_000)]
    public async Task UNSUBSCRIBE_ThenPUBLISH_DoesNotDeliverMessage()
    {
        await using var cluster = await TestcontainersRedisCluster.StartAsync();
        var (host, port) = cluster.MasterEndpoint;
        var channel = $"pubsub:unsubscribe:{Guid.NewGuid():N}";

        await using var subscriber = await RedisRespClient.ConnectAsync(host, port);
        await using var publisher = await RedisRespClient.ConnectAsync(host, port);

        Assert.Equal(
            RespBuilder.InitArray(3) +
            RespBuilder.BulkString("subscribe") +
            RespBuilder.BulkString(channel) +
            RespBuilder.Integer(1),
            await subscriber.ExecuteCommandAsync("SUBSCRIBE", channel));

        Assert.Equal(
            RespBuilder.InitArray(3) +
            RespBuilder.BulkString("unsubscribe") +
            RespBuilder.BulkString(channel) +
            RespBuilder.Integer(0),
            await subscriber.ExecuteCommandAsync("UNSUBSCRIBE", channel));

        Assert.Equal(RespBuilder.Integer(0), await publisher.ExecuteCommandAsync("PUBLISH", channel, "hello"));

        using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => subscriber.ReadResponseAsync(timeout.Token));
    }
}
