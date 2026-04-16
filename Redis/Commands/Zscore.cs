using Redis.Cache;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Zscore : Base
{
    protected override string Name => nameof(Zscore);
    public override bool CanBePropagated => false;
    
    protected override Task<string> ExecuteCore(CommandContext commandContext)
    {
        var commands = commandContext.CommandDetails.CommandParts;
        
        var key = commands[4];
        var member = commands[6];
        
        var result = DataCache.Zscore(key, member);
        
        var resp = result != null 
            ? RespBuilder.BulkString(result.ToString())
            : RespBuilder.Null();
        
        commandContext.Socket.SendCommand(resp);
        return Task.FromResult(resp);
    }
}
