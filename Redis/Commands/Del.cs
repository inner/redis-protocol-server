using Redis.Cache;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Del : Base
{
    protected override string Name => nameof(Del);
    public override bool CanBePropagated => true;

    protected override Task<string> ExecuteCore(CommandContext commandContext)
    {
        var keys = commandContext.CommandDetails.CommandParts[3..]
            .Where(x => !x.StartsWith("$"))
            .Distinct();

        var count = DataCache.DelKeys(keys.ToArray());
        var result = RespBuilder.Integer(count);
        commandContext.Socket.SendCommand(result);
        return Task.FromResult(result);
    }
}
