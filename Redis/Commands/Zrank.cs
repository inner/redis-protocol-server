using Redis.Cache;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Zrank : Base
{
    protected override string Name => nameof(Zrank);
    public override bool CanBePropagated => false;
    
    protected override Task<string> ExecuteCore(CommandContext commandContext)
    {
        var commands = commandContext.CommandDetails.CommandParts;
        
        var key = commands[4];
        var member = commands[6];
        
        var result = DataCache.Zrank(key, member);

        var resp = result == null
            ? RespBuilder.Null()
            : RespBuilder.Integer(result.Value);
        
        commandContext.Socket.SendCommand(resp);
        return Task.FromResult(resp);
    }
}
