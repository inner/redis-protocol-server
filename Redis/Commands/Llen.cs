using Redis.Cache;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Llen : Base
{
    protected override string Name => nameof(Llen);
    public override bool CanBePropagated => false;

    protected override Task<string> ExecuteCore(CommandContext commandContext)
    {
        var commands = commandContext.CommandDetails.CommandParts;

        var key = commands[4];

        var result = DataCache.Llen(key);
        var resp = RespBuilder.Integer(result);
        
        commandContext.Socket.SendCommand(resp);
        return Task.FromResult(resp);
    }
}
