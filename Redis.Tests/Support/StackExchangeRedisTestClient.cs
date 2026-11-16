using StackExchange.Redis;

namespace Redis.Tests.Support;

internal static class StackExchangeRedisTestClient
{
    public static async Task<ConnectionMultiplexer> ConnectAsync(
        string host,
        int port,
        Func<Task<string>> diagnosticsFactory)
    {
        using var connectionLogs = new StringWriter();

        try
        {
            return await ConnectionMultiplexer.ConnectAsync(CreateConfiguration(host, port), connectionLogs);
        }
        catch (RedisConnectionException ex)
        {
            throw new Xunit.Sdk.XunitException(
                $"StackExchange.Redis failed to connect.{Environment.NewLine}" +
                $"Client logs:{Environment.NewLine}{connectionLogs}{Environment.NewLine}" +
                $"{await diagnosticsFactory()}",
                ex);
        }
    }

    public static async Task EventuallyAsync(
        Func<Task<bool>> predicate,
        TimeSpan timeout,
        Func<Task<string>> diagnosticsFactory)
    {
        var timeoutAt = DateTimeOffset.UtcNow.Add(timeout);

        while (DateTimeOffset.UtcNow < timeoutAt)
        {
            if (await predicate())
            {
                return;
            }

            await Task.Delay(100);
        }

        throw new TimeoutException(
            $"Timed out waiting for the expected Redis state.{Environment.NewLine}{await diagnosticsFactory()}");
    }

    private static ConfigurationOptions CreateConfiguration(string host, int port)
    {
        // Default client behavior, including its handshake and topology
        // probes. Commands the server does not implement (e.g. CLUSTER) get
        // an error reply, which the client tolerates.
        var options = new ConfigurationOptions
        {
            AbortOnConnectFail = false,
            ConnectTimeout = 5_000,
            SyncTimeout = 5_000,
            AsyncTimeout = 5_000,
        };

        options.EndPoints.Add(host, port);
        return options;
    }
}
