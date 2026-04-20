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
        // StackExchange.Redis normally performs extra topology and feature probes.
        // This Redis implementation does not support the full discovery surface and
        // does not safely parse pipelined handshake traffic, so we force the client
        // into a thin proxy-style mode against the explicit endpoint.
        var options = new ConfigurationOptions
        {
            AbortOnConnectFail = false,
            ClientName = string.Empty,
            ConfigCheckSeconds = 0,
            ConfigurationChannel = string.Empty,
            CommandMap = CommandMap.Twemproxy,
            Proxy = Proxy.Twemproxy,
            SetClientLibrary = false,
            TieBreaker = string.Empty,
            ConnectTimeout = 5_000,
            SyncTimeout = 5_000,
            AsyncTimeout = 5_000,
        };

        options.EndPoints.Add(host, port);
        return options;
    }
}
