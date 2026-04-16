using Redis.Cache;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Incr : Base
{
    protected override string Name => nameof(Incr);
    public override bool CanBePropagated => true;

    protected override Task<string> ExecuteCore(CommandContext commandContext)
    {
        string result;
        var key = commandContext.CommandDetails.CommandParts[4];

        var cacheItem = DataCache.Get(key);
        if (cacheItem == null)
        {
            DataCache.Set(key, "1");
            result = RespBuilder.Integer(1);
            SendIfNotFromTransaction(commandContext, result);
            return Task.FromResult(result);
        }

        if (!long.TryParse(cacheItem.Value, out var longValue))
        {
            result = RespBuilder.Error("value is not an integer or out of range");
            SendIfNotFromTransaction(commandContext, result);
            return Task.FromResult(result);
        }

        longValue++;
        DataCache.Set(key, longValue.ToString());
        result = RespBuilder.Integer(longValue);
        SendIfNotFromTransaction(commandContext, result);
        return Task.FromResult(result);
    }
}
