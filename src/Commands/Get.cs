using codecrafters_redis.Cache;
using codecrafters_redis.Common;

namespace codecrafters_redis.Commands;

public class Get : Base
{
    public override bool CanBePropagated => false;

    protected override async Task<string> OnMasterNodeExecute(CommandContext commandContext)
    {
        return await GenerateCommonResponse(commandContext);
    }

    protected override async Task<string> OnReplicaNodeExecute(CommandContext commandContext)
    {
        return await GenerateCommonResponse(commandContext);
    }

    private static Task<string> GenerateCommonResponse(CommandContext commandContext)
    {
        var cacheKey = commandContext.CommandDetails.CommandParts[4];
        var cacheItem = DataCache.Get(cacheKey);

        var response = cacheItem is null or { Value: null }
            ? RespBuilder.Null()
            : RespBuilder.BulkString(cacheItem.Value);

        if (commandContext is { ReplicaConnection: false, CommandDetails.FromTransaction: false })
        {
            commandContext.Socket.Send(response.AsBytes());
        }
        
        return Task.FromResult(RespBuilder.SimpleString(cacheItem?.Value!));
    }
}