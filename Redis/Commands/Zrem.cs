using Redis.Cache;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Zrem : Base
{
    protected override string Name => nameof(Zrem);
    public override bool CanBePropagated => true;
    
    protected override Task<string> ExecuteCore(CommandContext commandContext)
    {
        var commands = commandContext.CommandDetails.CommandParts;
        
        var key = commands[4];
        var member = commands[6];
        
        var result = DataCache.Zrem(key, member);
        var resp = RespBuilder.Integer(result);
        
        commandContext.Socket.SendCommand(resp);
        return Task.FromResult(resp);
    }
}
