using Redis.Cache;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Zcard : Base
{
    protected override string Name => nameof(Zcard);
    public override bool CanBePropagated => false;
    
    protected override Task<string> ExecuteCore(CommandContext commandContext)
    {
        var commands = commandContext.CommandDetails.CommandParts;
        
        var key = commands[4];
        
        var result = DataCache.Zcard(key);
        var resp = RespBuilder.Integer(result);
        
        Reply(commandContext, resp);
        return Task.FromResult(resp);
    }
}
