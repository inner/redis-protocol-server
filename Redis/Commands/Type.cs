using Redis.Cache;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Type : Base
{
    protected override string Name => nameof(Type);
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
                commandContext.Socket.SendCommand(result);
                return Task.FromResult(result);
            }

            var streamCacheItem = fetchItem.Deserialize<StreamCacheItem>();
            if (streamCacheItem != null && string.Equals(streamCacheItem.Type, nameof(StreamCacheItem),
                    StringComparison.InvariantCultureIgnoreCase))
            {
                result = RespBuilder.SimpleString("stream");
                if (!commandContext.ReplicaConnection)
                {
                    commandContext.Socket.SendCommand(result);
                }

                return Task.FromResult(result);
            }
        }

        result = RespBuilder.SimpleString("none");
        commandContext.Socket.SendCommand(result);
        return Task.FromResult(result);
    }
    
    public override Dictionary<string, Dictionary<string, string>> Docs()
    {
        return new()
        {
            {
                Name,
                new()
                {
                    { "summary", "Determines the type of value stored at a key." },
                    { "usage #1", "redis-cli SET mykey1 myval1" },
                    { "usage #2", "redis-cli TYPE mykey1" },
                    { "usage #4", "redis-cli TYPE nosuchkey" },
                    { "usage #5", "redis-cli XADD stream1 * mykey1 myval1" },
                    { "usage #6", "redis-cli TYPE stream1" }
                }
            }
        };
    }
}