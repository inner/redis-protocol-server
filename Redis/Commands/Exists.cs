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
        // A key named twice counts twice, as in Redis.
        var keys = commandContext.CommandDetails.CommandParts
            .Skip(4)
            .Where((_, i) => i % 2 == 0);

        var count = DataCache.CountKeys(keys.ToArray());
        var result = RespBuilder.Integer(count);
        Reply(commandContext, result);
        return Task.FromResult(result);
    }
}
