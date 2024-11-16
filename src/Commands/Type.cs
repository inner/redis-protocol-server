using codecrafters_redis.Cache;
using codecrafters_redis.Commands.Common;
using codecrafters_redis.Common;

namespace codecrafters_redis.Commands;

public class Type : Base
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
        string result;

        var key = commandContext.CommandDetails.CommandParts[4];
        var fetchItem = DataCache.Fetch(key);

        if (fetchItem != null)
        {
            var basicCacheItem = fetchItem.Deserialize<BasicCacheItem>();
            if (basicCacheItem != null && string.Equals(basicCacheItem.Type, nameof(BasicCacheItem),
                    StringComparison.InvariantCultureIgnoreCase))
            {
                result = RespBuilder.SimpleString("string");
                commandContext.Socket.Send(result.AsBytes());
                return Task.FromResult(result);
            }

            var streamCacheItem = fetchItem.Deserialize<StreamCacheItem>();
            if (streamCacheItem != null && string.Equals(streamCacheItem.Type, nameof(StreamCacheItem),
                    StringComparison.InvariantCultureIgnoreCase))
            {
                result = RespBuilder.SimpleString("stream");
                if (!commandContext.ReplicaConnection)
                {
                    commandContext.Socket.Send(result.AsBytes());
                }

                return Task.FromResult(result);
            }
        }

        result = RespBuilder.SimpleString("none");
        commandContext.Socket.Send(result.AsBytes());
        return Task.FromResult(result);
    }
}