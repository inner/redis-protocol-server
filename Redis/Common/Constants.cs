namespace Redis.Common;

public static class Constants
{
    // regardless of the platform, the newline character is always \r\n
    // as per the RESP protocol
    public const string VerbatimNewLine = @"\r\n";
    public const string NewLine = "\r\n";
    
    public const int DefaultRedisPort = 6379;
    public const string WindowsMasterDir = @"C:\redis-rdb";
    public const string WindowsReplicaDir = @"C:\redis-rdb\replica";
    public const string LinuxMasterDir = "/tmp/redis-rdb";
    public const string LinuxReplicaDir = "/tmp/redis-rdb/replica";
}