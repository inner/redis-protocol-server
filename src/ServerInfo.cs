namespace codecrafters_redis;

public static class ServerInfo
{
    public static bool IsMaster { get; set; } = true;
    public static string? MasterReplId { get; set; }
    public static int MasterReplOffset  { get; set; }
    public static int Port { get; set; } = 6379;
    public static string? MasterHost { get; set; } = null;
    public static int? MasterPort { get; set; } = null;
}