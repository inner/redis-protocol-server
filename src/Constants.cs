namespace codecrafters_redis;

public static class Constants
{
    public static readonly int DefaultRedisPort = 6379;
    public static readonly string OkResponse = "+OK\r\n";
    public static readonly string OkArrayResponse = "*1\r\n+OK\r\n";
    public static readonly string PongResponse = "+PONG\r\n";
    public static readonly string NullResponse = "$-1\r\n";
}