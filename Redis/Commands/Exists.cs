using Redis.Cache;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

// spec: https://redis.io/docs/latest/commands/exists/
public class Exists : Base
{
    protected override string Name => nameof(Exists);
    public override bool CanBePropagated => false;

    protected override Task<string> ExecuteCore(CommandContext commandContext)
    {
        var keys = commandContext.CommandDetails.CommandParts[3..]
            .Where(x => !x.StartsWith("$"))
            .Distinct();

        var count = DataCache.CountKeys(keys.ToArray());
        var result = RespBuilder.Integer(count);
        commandContext.Socket.SendCommand(result);
        return Task.FromResult(result);
    }
}
