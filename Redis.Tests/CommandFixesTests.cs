namespace Redis.Tests;

// Regression tests for command-level bugs: missing or duplicate replies,
// argument parsing, units and authentication.
public class CommandFixesTests
{
    [DockerFact(Timeout = 60_000)]
    public async Task Keyspace_EdgeCases_ReplyOnceWithRedisSemantics()
    {
        await using var cluster = await TestcontainersRedisCluster.StartAsync();
        var (host, port) = cluster.MasterEndpoint;
        var id = Guid.NewGuid().ToString("N");

        await using var client = await RedisRespClient.ConnectAsync(host, port);

        // LPOP on a missing list used to send nothing at all.
        Assert.Equal(RespBuilder.Null(), await client.ExecuteCommandAsync("LPOP", $"missing:{id}"));

        // KEYS without matches used to send two replies.
        Assert.Equal(RespBuilder.EmptyArray(), await client.ExecuteCommandAsync("KEYS", $"none:{id}:*"));
        Assert.Equal(RespBuilder.SimpleString("PONG"), await client.ExecuteCommandAsync("PING"));

        // A key named twice counts twice; keys may start with '$'.
        var dollarKey = $"$dollar:{id}";
        await client.ExecuteCommandAsync("SET", dollarKey, "v");
        Assert.Equal(RespBuilder.Integer(2), await client.ExecuteCommandAsync("EXISTS", dollarKey, dollarKey));
        Assert.Equal(RespBuilder.Integer(1), await client.ExecuteCommandAsync("DEL", dollarKey));

        // SET EX and unknown options.
        Assert.Equal(RespBuilder.SimpleString("OK"), await client.ExecuteCommandAsync("SET", $"ex:{id}", "v", "EX", "100"));
        Assert.Equal(RespBuilder.Error("syntax error"), await client.ExecuteCommandAsync("SET", $"ex:{id}", "v", "NOPE", "1"));
    }

    [DockerFact(Timeout = 60_000)]
    public async Task GEODIST_And_GEOSEARCH_HonourUnits()
    {
        await using var cluster = await TestcontainersRedisCluster.StartAsync();
        var (host, port) = cluster.MasterEndpoint;
        var key = $"geo:{Guid.NewGuid():N}";

        await using var client = await RedisRespClient.ConnectAsync(host, port);
        await client.ExecuteCommandAsync("GEOADD", key, "13.361389", "38.115556", "Palermo");
        await client.ExecuteCommandAsync("GEOADD", key, "15.087269", "37.502669", "Catania");

        Assert.Equal(RespBuilder.BulkString("166274.1516"), await client.ExecuteCommandAsync("GEODIST", key, "Palermo", "Catania"));
        Assert.Equal(RespBuilder.BulkString("166.2742"), await client.ExecuteCommandAsync("GEODIST", key, "Palermo", "Catania", "km"));
        Assert.Equal(RespBuilder.Null(), await client.ExecuteCommandAsync("GEODIST", key, "Palermo", "Atlantis"));
        Assert.Equal(
            RespBuilder.InitArray(1) + RespBuilder.BulkString("Catania"),
            await client.ExecuteCommandAsync("GEOSEARCH", key, "FROMLONLAT", "15", "37", "BYRADIUS", "100", "km"));
        Assert.Equal(
            RespBuilder.Error("unsupported unit provided. please use M, KM, FT, MI"),
            await client.ExecuteCommandAsync("GEODIST", key, "Palermo", "Catania", "parsec"));
    }

    [DockerFact(Timeout = 60_000)]
    public async Task COMMAND_COUNT_And_LIST_DescribeSupportedCommands()
    {
        await using var cluster = await TestcontainersRedisCluster.StartAsync();
        var (host, port) = cluster.MasterEndpoint;

        await using var client = await RedisRespClient.ConnectAsync(host, port);

        var count = await client.ExecuteCommandAsync("COMMAND", "COUNT");
        var list = await client.ExecuteCommandAsync("COMMAND", "LIST");

        Assert.StartsWith(":", count);
        Assert.StartsWith($"*{count.Trim()[1..]}\r\n", list);
        Assert.Contains(RespBuilder.BulkString("psetex"), list);
    }

    [DockerFact(Timeout = 60_000)]
    public async Task DefaultUserPassword_GatesNewConnectionsOnly()
    {
        await using var cluster = await TestcontainersRedisCluster.StartAsync();
        var (host, port) = cluster.MasterEndpoint;
        const string noAuth = "-NOAUTH Authentication required.\r\n";

        await using var admin = await RedisRespClient.ConnectAsync(host, port);
        await using var openedBefore = await RedisRespClient.ConnectAsync(host, port);
        Assert.Equal(RespBuilder.SimpleString("PONG"), await openedBefore.ExecuteCommandAsync("PING"));

        Assert.Equal(RespBuilder.SimpleString("OK"), await admin.ExecuteCommandAsync("ACL", "SETUSER", "default", ">s3cret"));

        // Connections opened before the password stay logged in, as in Redis.
        Assert.Equal(RespBuilder.SimpleString("PONG"), await openedBefore.ExecuteCommandAsync("PING"));

        // New connections must AUTH for every command, not only ACL WHOAMI.
        await using var first = await RedisRespClient.ConnectAsync(host, port);
        await using var second = await RedisRespClient.ConnectAsync(host, port);
        Assert.Equal(noAuth, await first.ExecuteCommandAsync("GET", "anything"));
        Assert.Equal(noAuth, await first.ExecuteCommandAsync("ACL", "WHOAMI"));

        // Legacy single-argument AUTH logs in as `default`; two connections
        // can be logged in as the same user at once.
        Assert.Equal(RespBuilder.SimpleString("OK"), await first.ExecuteCommandAsync("AUTH", "s3cret"));
        Assert.Equal(RespBuilder.SimpleString("OK"), await second.ExecuteCommandAsync("AUTH", "default", "s3cret"));
        Assert.Equal(RespBuilder.BulkString("default"), await first.ExecuteCommandAsync("ACL", "WHOAMI"));
        Assert.Equal(RespBuilder.SimpleString("PONG"), await second.ExecuteCommandAsync("PING"));
    }
}
