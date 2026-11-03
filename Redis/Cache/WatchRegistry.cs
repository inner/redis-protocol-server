using System.Collections.Concurrent;
using System.Net.Sockets;

namespace Redis.Cache;

// ==============================================================================
// Optimistic locking (WATCH) bookkeeping
// ==============================================================================

// Connection state is not threaded through the command pipeline as an object,
// so watched keys are kept here, keyed by the connection's socket. Each watched
// key remembers the key version observed at WATCH time; EXEC can later compare
// it with the current version to detect modifications by any client.
public static class WatchRegistry
{
    // Monotonic per-key version, bumped by every write that goes through
    // DataCache. Keys never written have version 0.
    private static readonly ConcurrentDictionary<string, long> KeyVersions = new();

    private static readonly ConcurrentDictionary<Socket, ConcurrentDictionary<string, long>> WatchedKeys = new();

    public static void Touch(string key)
    {
        KeyVersions.AddOrUpdate(key, 1, (_, version) => version + 1);
    }

    public static void Watch(Socket socket, string key)
    {
        var keys = WatchedKeys.GetOrAdd(socket, _ => new ConcurrentDictionary<string, long>());

        // Re-watching an already watched key keeps the original snapshot, so a
        // modification between the two WATCH calls still aborts the transaction.
        keys.TryAdd(key, KeyVersions.GetValueOrDefault(key));
    }

    public static IReadOnlyCollection<string> GetWatchedKeys(Socket socket)
    {
        return WatchedKeys.TryGetValue(socket, out var keys)
            ? keys.Keys.ToList()
            : [];
    }
}
