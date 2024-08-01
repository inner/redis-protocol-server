using System.Net.Sockets;
using System.Text;
using codecrafters_redis.Cache;
using codecrafters_redis.Common;
using codecrafters_redis.Receivers;

namespace codecrafters_redis.Commands;

public class Type : Base
{
    public override bool CanBePropagated => false;

    protected override async Task<string> OnMasterNodeExecute(Socket socket, CommandDetails commandDetails,
        List<CommandQueueItem> commandQueue, ReceiverBase receiver, bool replicaConnection = false)
    {
        return await GenerateCommonResponse(socket, commandDetails, replicaConnection);
    }

    protected override async Task<string> OnReplicaNodeExecute(Socket socket, CommandDetails commandDetails,
        List<CommandQueueItem> commandQueue, ReceiverBase receiver, bool replicaConnection = false)
    {
        return await GenerateCommonResponse(socket, commandDetails, replicaConnection);
    }

    private static Task<string> GenerateCommonResponse(Socket socket, CommandDetails commandDetails,
        bool replicaConnection = false)
    {
        string result;

        var key = commandDetails.CommandParts[4];
        var fetchItem = DataCache.Fetch(key);

        if (fetchItem != null)
        {
            var basicCacheItem = fetchItem.Deserialize<BasicCacheItem>();
            if (basicCacheItem != null && string.Equals(basicCacheItem.Type, nameof(BasicCacheItem),
                    StringComparison.InvariantCultureIgnoreCase))
            {
                result = "+string\r\n";
                socket.Send(Encoding.UTF8.GetBytes(result));
                return Task.FromResult(result);
            }

            var streamCacheItem = fetchItem.Deserialize<StreamCacheItem>();
            if (streamCacheItem != null && string.Equals(streamCacheItem.Type, nameof(StreamCacheItem),
                    StringComparison.InvariantCultureIgnoreCase))
            {
                result = "+stream\r\n";
                if (!replicaConnection)
                {
                    socket.Send(Encoding.UTF8.GetBytes(result));
                }

                return Task.FromResult(result);
            }
        }

        result = "+none\r\n";
        socket.Send(Encoding.UTF8.GetBytes(result));
        return Task.FromResult(result);
    }
}