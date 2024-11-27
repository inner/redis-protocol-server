using Redis.Cache;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Get : Base
{
    public override bool CanBePropagated => false;
    
    public override Dictionary<string, Dictionary<string, string>> Docs()
    {
        return new()
        {
            {
                "GET",
                new()
                {
                    { "summary", "Returns the string value of a key." },
                    { "usage", "redis-cli GET key1" }
                }
            }
        };
    }

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
            commandContext.Socket.SendCommand(response);
        }
        
        return Task.FromResult(RespBuilder.SimpleString(cacheItem?.Value!));
    }
}