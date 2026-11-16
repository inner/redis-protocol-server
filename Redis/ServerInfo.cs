using System.Collections.Concurrent;
using System.Net.Sockets;
using Redis.Cache;
using Redis.Common;

namespace Redis;

public static class ServerInfo
{
    public const int DefaultRedisPort = 6379;
    public const string RdbExtension = ".rdb";

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

    // Password a replica sends (AUTH) to its master during the handshake.
    public string? MasterAuth { get; set; }
    public bool DbFileExists { get; set; }
    public AofOptions Aof { get; } = new();
    public int ConnectedReplicasCount => Replicas.Count(x => x.Value.Connected);

    public static async Task ExecuteOnReplicas(string resp)
    {
        var connectedReplicas = ServerInfo.ServerRuntimeContext.Replicas
            .Where(x => x.Value.Connected);

        // A replica that went away must not fail the client's command; its
        // entry is removed when its connection handler exits.
        var tasks = connectedReplicas
            .Select(replica =>
                Task.Run(() =>
                {
                    try
                    {
                        replica.Value.SendCommand(resp);
                    }
                    catch (Exception ex) when (ex is SocketException or ObjectDisposedException)
                    {
                        Console.WriteLine($"Failed to propagate to replica {replica.Key}: {ex.Message}");
                    }
                }));

        await Task.WhenAll(tasks);
    }

    public static bool DefaultUserAuthenticated => DataCache.DefaultUserExists();
}

// Append-only-file settings with Redis' defaults; overridable via CLI flags.
public class AofOptions
{
    public string AppendOnly { get; set; } = "no";
    public string AppendDirName { get; set; } = "appendonlydir";
    public string AppendFilename { get; set; } = "appendonly.aof";
    public string AppendFsync { get; set; } = "everysec";
}

public class Replication
{
    public bool ReplicaHandshakeCompleted { get; set; }
    public int ReplicaBytesReceived { get; private set; }
    public int ReplicaAcksReceived { get; set; }
    public void IncrementReplicaAcksReceived() => ReplicaAcksReceived += 1;
    public void IncrementReplicaBytesReceived(int bytesReceived) => ReplicaBytesReceived += bytesReceived;
}