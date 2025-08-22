using Redis.Cache;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Get : Base
{
    protected override string Name => nameof(Get);
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

        string response;
        
        if (cacheItem?.Value == null)
        {
            response = RespBuilder.Null();
            Send(commandContext, response);
            return Task.FromResult(response);
        }

        response = RespBuilder.BulkString(cacheItem.Value);
        Send(commandContext, response);
        return Task.FromResult(response);
    }

    private static void Send(CommandContext commandContext, string response)
    {
        if (commandContext is { CommandDetails.FromTransaction: false })
        {
            commandContext.Socket.SendCommand(response);
        }
    }
    
    public override Dictionary<string, Dictionary<string, string>> Docs()
    {
        return new()
        {
            {
                Name,
                new()
                {
                    { "summary", "Returns the string value of a key." },
                    { "usage", "redis-cli GET key1" }
                }
            }
        };
    }
}