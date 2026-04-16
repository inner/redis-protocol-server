using Redis.Cache;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Rpush : Base
{
    protected override string Name => nameof(Rpush);
    public override bool CanBePropagated => true;

    protected override Task<string> ExecuteCore(CommandContext commandContext)
    {
        var commands = commandContext.CommandDetails.CommandParts;

        var key = commands[4];
        var values = commands.Skip(6)
            .Where((_, i) => i % 2 == 0)
            .ToArray();

        var result = DataCache.Rpush(key, values);
        var resp = RespBuilder.Integer(result);
        commandContext.Socket.SendCommand(resp);

        return Task.FromResult(resp);
    }
}
