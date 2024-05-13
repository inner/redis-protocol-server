using System.Collections.Concurrent;
using System.Net.Sockets;

namespace codecrafters_redis;

public static class ServerInfo
{
    private static readonly object LockObject = new();
    public static bool IsMaster { get; set; } = true;
    public static string? MasterReplId { get; set; }
    public static int MasterReplOffset { get; set; }
    public static readonly ConcurrentDictionary<string, Socket> Replicas = new();
    public static bool ReplicaHandshakeCompleted { get; set; }
    public static bool ReplicaFirstByteReceived { get; set; }
    public static int ReplicaBytesReceived { get; set; }
    
    public static int ReplicaAcksReceived { get; set; }
    
    public static void IncrementReplicaAcksReceived()
    {
        lock (LockObject)
        {
            ReplicaAcksReceived += 1;
        }
    }

    public static void IncrementReplicaBytesReceived(int bytesReceived)
    {
        lock (LockObject)
        {
            ReplicaBytesReceived += bytesReceived;
        }
    }

    public static int GetConnectedReplicas()
    {
        lock (LockObject)
        {
            return Replicas.Count(x => x.Value.Connected);
        }
    }
}