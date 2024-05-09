using System.Collections.Concurrent;
using System.Net.Sockets;

namespace codecrafters_redis;

public static class ServerInfo
{
    private static readonly object LockObject = new();
    public static bool IsMaster { get; set; } = true;
    public static string? MasterReplId { get; set; }
    public static int MasterReplOffset  { get; set; }
    public static readonly ConcurrentDictionary<string, Socket> Replicas = new();
    public static int ConnectedReplicas { get; set; } = Replicas.Count(x => x.Value.Connected);
    public static bool ReplicaHandshakeCompleted { get; set; }
    public static bool FirstByteReceived { get; set; }
    public static int BytesReceived { get; set; }
    public static void IncrementBytesReceived(int bytesReceived)
    {
        lock (LockObject)
        {
            BytesReceived += bytesReceived;
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