using System.Collections.Concurrent;
using System.Net.Sockets;

namespace codecrafters_redis;

public static class ServerInfo
{
    public static bool IsMaster { get; set; } = true;
    public static string? MasterReplId { get; set; }
    public static int MasterReplOffset  { get; set; }
    public static readonly ConcurrentDictionary<string, Socket> Replicas = new();
    public static bool ReplicaHandshakeCompleted { get; set; }
    public static int BytesRead { get; set; }
}