using Redis.Cache;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Geoadd : Base
{
    protected override string Name => nameof(Geoadd);
    public override bool CanBePropagated => true;
    
    protected override Task<string> ExecuteCore(CommandContext commandContext)
    {
        var commands = commandContext.CommandDetails.CommandParts;
        
        var key = commands[4];
        var longitude = commands[6];
        var latitude = commands[8];
        var member = commands[10];
        
        var result = DataCache.Geoadd(key, double.Parse(longitude), double.Parse(latitude), member);
        var resp = RespBuilder.Integer(result);
        
        commandContext.Socket.SendCommand(resp);
        return Task.FromResult(resp);
    }
}
