using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace Redis;

public static class ServerInfo
{
    public const int DefaultRedisPort = 6379;
    public const string WindowsMasterDir = @"C:\redis-rdb";
    public const string WindowsReplicaDir = @"C:\redis-rdb\replica";
    public const string LinuxMasterDir = "/tmp/redis-rdb";
    public const string LinuxReplicaDir = "/tmp/redis-rdb/replica";
    
    public static OSPlatform OperatingSystem =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? OSPlatform.Windows
            : OSPlatform.Linux;

    public static readonly ServerRuntimeContext ServerRuntimeContext = new();
    public static Replication Replication { get; } = new();
}

public class ServerRuntimeContext
{
    private static readonly object ReplicasLockObject = new();
    public bool IsMaster { get; set; } = true;
    public string MasterReplId { get; set; } = string.Empty;
    public readonly ConcurrentDictionary<string, Socket> Replicas = new();
    public string DataDir { get; set; } = null!;
    public string DbFilename { get; set; } = null!;
    public bool DbFileExists { get; set; }

    public int GetConnectedReplicas()
    {
        lock (ReplicasLockObject)
        {
            return Replicas.Count(x => x.Value.Connected);
        }
    }
}

public class Replication
{
    private static readonly object ReplicaAcksReceivedLockObject = new();
    private static readonly object ReplicaBytesReceivedLockObject = new();
    public bool ReplicaHandshakeCompleted { get; set; }
    public bool ReplicaFirstByteReceived { get; set; }
    public int ReplicaBytesReceived { get; private set; }
    public int ReplicaAcksReceived { get; set; }

    public void IncrementReplicaAcksReceived()
    {
        lock (ReplicaAcksReceivedLockObject)
        {
            ReplicaAcksReceived += 1;
        }
    }

    public void IncrementReplicaBytesReceived(int bytesReceived)
    {
        lock (ReplicaBytesReceivedLockObject)
        {
            ReplicaBytesReceived += bytesReceived;
        }
    }
}