using System.Net.Sockets;
using System.Text;
using codecrafters_redis.Cache;
using codecrafters_redis.Receivers;

namespace codecrafters_redis.Commands;

public class Incr : Base
{
    public override bool CanBePropagated => true;

    protected override async Task<string> OnMasterNodeExecute(Socket socket, CommandDetails commandDetails,
        List<CommandQueueItem> commandQueue, ReceiverBase receiver, bool replicaConnection = false)
    {
        return await GenerateCommonResponse(socket, commandDetails);
    }

    protected override async Task<string> OnReplicaNodeExecute(Socket socket, CommandDetails commandDetails,
        List<CommandQueueItem> commandQueue, ReceiverBase receiver, bool replicaConnection = false)
    {
        return await GenerateCommonResponse(socket, commandDetails);
    }

    private Task<string> GenerateCommonResponse(Socket socket, CommandDetails commandDetails)
    {
        string result;
        var key = commandDetails.CommandParts[4];

        var cacheItem = DataCache.Get(key);
        if (cacheItem == null)
        {
            DataCache.Set(key, "1");
            result = ":1\r\n";

            if (!commandDetails.FromTransaction)
            {
                socket.Send(Encoding.UTF8.GetBytes(result));
            }

            return Task.FromResult(result);
        }

        if (!long.TryParse(cacheItem.Value, out var longValue))
        {
            result = "-ERR value is not an integer or out of range\r\n";

            if (!commandDetails.FromTransaction)
            {
                socket.Send(Encoding.UTF8.GetBytes(result));
            }

            return Task.FromResult(result);
        }

        longValue++;
        DataCache.Set(key, longValue.ToString());
        result = $":{longValue}\r\n";

        if (!commandDetails.FromTransaction)
        {
            socket.Send(Encoding.UTF8.GetBytes(result));
        }

        return Task.FromResult(result);
    }
}