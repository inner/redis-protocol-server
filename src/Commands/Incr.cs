using System.Net.Sockets;
using System.Text;
using codecrafters_redis.Cache;

namespace codecrafters_redis.Commands;

public class Incr : Base
{
    public override bool CanBePropagated => true;

    protected override Task OnMasterNodeExecute(Socket socket, int commandCount, string[] commandParts,
        bool replicaConnection = false)
    {
        GenerateCommonResponse(socket, commandParts, replicaConnection);
        return Task.CompletedTask;
    }

    protected override Task OnReplicaNodeExecute(Socket socket, int commandCount, string[] commandParts,
        bool replicaConnection = false)
    {
        GenerateCommonResponse(socket, commandParts, replicaConnection);
        return Task.CompletedTask;
    }

    private static void GenerateCommonResponse(Socket socket, string[] commandParts, bool replicaConnection = false)
    {
        var key = commandParts[4];
        
        var cacheItem = DataCache.Get(key);
        if (cacheItem == null)
        {
            DataCache.Set(key, "1");
            socket.Send(":1\r\n"u8.ToArray());
            return;
        }

        if (!long.TryParse(cacheItem.Value, out var longValue))
        {
            socket.Send("-ERR value is not an integer or out of range\r\n"u8.ToArray());
            return;
        }
        
        longValue++;
        DataCache.Set(key, longValue.ToString());
        socket.Send(Encoding.UTF8.GetBytes($":{longValue}\r\n"));
    }
}