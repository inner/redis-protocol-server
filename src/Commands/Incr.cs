using System.Net.Sockets;
using System.Text;
using codecrafters_redis.Cache;
using codecrafters_redis.Receivers;

namespace codecrafters_redis.Commands;

public class Incr : Base
{
    public override bool CanBePropagated => true;

    protected override Task OnMasterNodeExecute(Socket socket, CommandDetails commandDetails,
        List<CommandQueueItem> commandQueue, ReceiverBase receiver, bool replicaConnection = false)
    {
        GenerateCommonResponse(socket, commandDetails);
        return Task.CompletedTask;
    }

    protected override Task OnReplicaNodeExecute(Socket socket, CommandDetails commandDetails,
        List<CommandQueueItem> commandQueue, ReceiverBase receiver, bool replicaConnection = false)
    {
        GenerateCommonResponse(socket, commandDetails);
        return Task.CompletedTask;
    }

    private static void GenerateCommonResponse(Socket socket, CommandDetails commandDetails)
    {
        var key = commandDetails.CommandParts[4];
        
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