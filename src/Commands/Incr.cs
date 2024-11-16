using codecrafters_redis.Cache;
using codecrafters_redis.Commands.Common;
using codecrafters_redis.Common;

namespace codecrafters_redis.Commands;

public class Incr : Base
{
    public override bool CanBePropagated => true;

    protected override async Task<string> OnMasterNodeExecute(CommandContext commandContext)
    {
        return await GenerateCommonResponse(commandContext);
    }

    protected override async Task<string> OnReplicaNodeExecute(CommandContext commandContext)
    {
        return await GenerateCommonResponse(commandContext);
    }

    private Task<string> GenerateCommonResponse(CommandContext commandContext)
    {
        string result;
        var key = commandContext.CommandDetails.CommandParts[4];

        var cacheItem = DataCache.Get(key);
        if (cacheItem == null)
        {
            DataCache.Set(key, "1");

            if (!commandContext.CommandDetails.FromTransaction)
            {
                commandContext.Socket.Send(RespBuilder.Integer(1).AsBytes());
            }

            return Task.FromResult(RespBuilder.Integer(1));
        }

        if (!long.TryParse(cacheItem.Value, out var longValue))
        {
            result = RespBuilder.Error("value is not an integer or out of range");

            if (!commandContext.CommandDetails.FromTransaction)
            {
                commandContext.Socket.Send(result.AsBytes());
            }

            return Task.FromResult(result);
        }

        longValue++;
        DataCache.Set(key, longValue.ToString());
        result = RespBuilder.Integer(longValue);

        if (!commandContext.CommandDetails.FromTransaction)
        {
            commandContext.Socket.Send(result.AsBytes());
        }

        return Task.FromResult(result);
    }
}