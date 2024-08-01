using System.Net.Sockets;
using System.Text;
using codecrafters_redis.Cache;
using codecrafters_redis.Common;
using codecrafters_redis.Receivers;

namespace codecrafters_redis.Commands;

public class Type : Base
{
    public override bool CanBePropagated => false;

    protected override Task OnMasterNodeExecute(Socket socket, CommandDetails commandDetails,
        List<CommandQueueItem> commandQueue, ReceiverBase receiver, bool replicaConnection = false)
    {
        return GenerateCommonResponse(socket, commandDetails, replicaConnection);
    }
    
    protected override Task OnReplicaNodeExecute(Socket socket, CommandDetails commandDetails,
        List<CommandQueueItem> commandQueue, ReceiverBase receiver, bool replicaConnection = false)
    {
        return GenerateCommonResponse(socket, commandDetails, replicaConnection);
    }

    private static Task GenerateCommonResponse(Socket socket, CommandDetails commandDetails, bool replicaConnection = false)
    {
        var key = commandDetails.CommandParts[4];
        var fetchItem = DataCache.Fetch(key);

        if (fetchItem != null)
        {
            var basicCacheItem = fetchItem.Deserialize<BasicCacheItem>();
            if (basicCacheItem != null && string.Equals(basicCacheItem.Type, nameof(BasicCacheItem),
                    StringComparison.InvariantCultureIgnoreCase))
            {
                socket.Send(Encoding.UTF8.GetBytes("+string\r\n"));
                return Task.CompletedTask;
            }

            var streamCacheItem = fetchItem.Deserialize<StreamCacheItem>();
            if (streamCacheItem != null && string.Equals(streamCacheItem.Type, nameof(StreamCacheItem),
                    StringComparison.InvariantCultureIgnoreCase))
            {
                if (!replicaConnection)
                {
                    socket.Send(Encoding.UTF8.GetBytes("+stream\r\n"));   
                }
                
                return Task.CompletedTask;
            }
        }

        socket.Send(Encoding.UTF8.GetBytes("+none\r\n"));
        return Task.CompletedTask;
    }
}