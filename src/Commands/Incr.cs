using System.Text;
using codecrafters_redis.Cache;
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
                commandContext.Socket.Send(":1\r\n"u8.ToArray());
            }

            return Task.FromResult("1".ConvertStringToSimpleResp());
        }

        if (!long.TryParse(cacheItem.Value, out var longValue))
        {
            result = "-ERR value is not an integer or out of range";

            if (!commandContext.CommandDetails.FromTransaction)
            {
                commandContext.Socket.Send(Encoding.UTF8.GetBytes($"{result}\r\n"));
            }

            return Task.FromResult(result);
        }

        longValue++;
        DataCache.Set(key, longValue.ToString());
        result = $":{longValue}";

        if (!commandContext.CommandDetails.FromTransaction)
        {
            commandContext.Socket.Send(Encoding.UTF8.GetBytes($"{result}\r\n"));
        }

        return Task.FromResult(result);
    }
}