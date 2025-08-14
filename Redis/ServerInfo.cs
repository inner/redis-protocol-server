using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Redis.Common;

namespace Redis;

public static class ServerInfo
{
    public const int DefaultRedisPort = 6379;
    public const string WindowsMasterDir = @"C:\redis-rdb";
    public const string WindowsReplicaDir = @"C:\redis-rdb\replica";
    public const string LinuxMasterDir = "/tmp/redis-rdb";
    public const string LinuxReplicaDir = "/tmp/redis-rdb/replica";
    public const string RdbExtension = ".rdb";

    public static OSPlatform OperatingSystem =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? OSPlatform.Windows
            : OSPlatform.Linux;

    public static readonly ServerRuntimeContext ServerRuntimeContext = new();
    public static Replication Replication { get; } = new();
}

public class ServerRuntimeContext
{
    public bool IsMaster { get; set; } = true;
    public string MasterReplId { get; set; } = string.Empty;
    public readonly ConcurrentDictionary<string, Socket> Replicas = new();
    public Socket? MasterSocket { get; set; }
    public string DataDir { get; set; } = null!;
    public string DbFilename { get; set; } = null!;
    public bool DbFileExists { get; set; }
    public int ConnectedReplicasCount => Replicas.Count(x => x.Value.Connected);

    public static async Task ExecuteOnReplicas(string resp)
    {
        var connectedReplicas = ServerInfo.ServerRuntimeContext.Replicas
            .Where(x => x.Value.Connected);

        var tasks = connectedReplicas
            .Select(replica =>
                Task.Run(() =>
                    replica.Value.SendCommand(resp)));

        await Task.WhenAll(tasks);
    }
}

public class Replication
{
    public bool ReplicaHandshakeCompleted { get; set; }
    public bool ReplicaFirstByteReceived { get; set; }
    public int ReplicaBytesReceived { get; private set; }
    public int ReplicaAcksReceived { get; set; }
    public void IncrementReplicaAcksReceived() => ReplicaAcksReceived += 1;
    public void IncrementReplicaBytesReceived(int bytesReceived) => ReplicaBytesReceived += bytesReceived;
}