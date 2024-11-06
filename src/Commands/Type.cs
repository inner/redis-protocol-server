using System.Text;
using codecrafters_redis.Cache;
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
                result = "+string\r\n";
                commandContext.Socket.Send(Encoding.UTF8.GetBytes(result));
                return Task.FromResult(result);
            }

            var streamCacheItem = fetchItem.Deserialize<StreamCacheItem>();
            if (streamCacheItem != null && string.Equals(streamCacheItem.Type, nameof(StreamCacheItem),
                    StringComparison.InvariantCultureIgnoreCase))
            {
                result = "+stream\r\n";
                if (!commandContext.ReplicaConnection)
                {
                    commandContext.Socket.Send(Encoding.UTF8.GetBytes(result));
                }

                return Task.FromResult(result);
            }
        }

        result = "+none\r\n";
        commandContext.Socket.Send(Encoding.UTF8.GetBytes(result));
        return Task.FromResult(result);
    }
}