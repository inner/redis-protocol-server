using System.Collections.Concurrent;
using System.Net.Sockets;

namespace codecrafters_redis.Network;

public static class SlaveServers
{
    public static ConcurrentDictionary<string, Socket> SlaveSockets = new();
}