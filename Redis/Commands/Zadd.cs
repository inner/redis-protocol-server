using Redis.Cache;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Zadd : Base
{
    protected override string Name => nameof(Zadd);
    public override bool CanBePropagated => true;

    protected override Task<string> ExecuteCore(CommandContext commandContext)
    {
        var commands = commandContext.CommandDetails.CommandParts;
        
        var key = commands[4];
        var score = commands[6];
        var member = commands[8];
        
        var result = DataCache.Zadd(key, double.Parse(score), member);
        var resp = RespBuilder.Integer(result);
        
        commandContext.Socket.SendCommand(resp);
        return Task.FromResult(resp);
    }
}
