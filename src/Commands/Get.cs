using System.Net.Sockets;
using System.Text;
using codecrafters_redis.Cache;
using codecrafters_redis.Common;
using codecrafters_redis.Receivers;

namespace codecrafters_redis.Commands;

public class Get : Base
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
        bool replicaConnection)
    {
        var cacheKey = commandDetails.CommandParts[4];
        var cacheItem = DataCache.Get(cacheKey);

        var response = cacheItem is null or { Value: null }
            ? Constants.NullResponse
            : $"${cacheItem.Value.Length}\r\n{cacheItem.Value}\r\n";

        if (!replicaConnection && !commandDetails.FromTransaction)
        {
            socket.Send(Encoding.UTF8.GetBytes(response));
        }
        
        return Task.FromResult(cacheItem?.Value.ConvertStringToSimpleResp())!;
    }
}