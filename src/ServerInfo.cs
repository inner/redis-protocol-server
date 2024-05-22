using System.Collections.Concurrent;
using System.Net.Sockets;

namespace codecrafters_redis;

public class ServerRuntimeContext
{
    private static readonly object LockObject = new();
    
    public bool IsMaster { get; set; } = true;
    public string? MasterReplId { get; set; }
    public int MasterReplOffset { get; set; }
    public readonly ConcurrentDictionary<string, Socket> Replicas = new();
    public string DataDir { get; set; } = null!;
    public string DbFilename { get; set; } = null!;
    
    public int GetConnectedReplicas()
    {
        lock (LockObject)
        {
            return Replicas.Count(x => x.Value.Connected);
        }
    }
}

public class Replication
{
    private static readonly object LockObject = new();
    public bool ReplicaHandshakeCompleted { get; set; }
    public bool ReplicaFirstByteReceived { get; set; }
    public int ReplicaBytesReceived { get; set; }
    public int ReplicaAcksReceived { get; set; }
    
    public void IncrementReplicaAcksReceived()
    {
        lock (LockObject)
        {
            ReplicaAcksReceived += 1;
        }
    }

    public void IncrementReplicaBytesReceived(int bytesReceived)
    {
        lock (LockObject)
        {
            ReplicaBytesReceived += bytesReceived;
        }
    }
}

public static class ServerInfo
{
    public static ServerRuntimeContext ServerRuntimeContext = new();
    public static Replication Replication { get; set; } = new();
}