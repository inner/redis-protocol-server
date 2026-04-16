using Redis.Cache;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Get : Base
{
    protected override string Name => nameof(Get);
    public override bool CanBePropagated => false;

    protected override Task<string> ExecuteCore(CommandContext commandContext)
    {
        var cacheKey = commandContext.CommandDetails.CommandParts[4];
        var cacheItem = DataCache.Get(cacheKey);

        string response;
        
        if (cacheItem?.Value == null)
        {
            response = RespBuilder.Null();
            SendIfNotFromTransaction(commandContext, response);
            return Task.FromResult(response);
        }

        response = RespBuilder.BulkString(cacheItem.Value);
        SendIfNotFromTransaction(commandContext, response);
        return Task.FromResult(response);
    }
}
