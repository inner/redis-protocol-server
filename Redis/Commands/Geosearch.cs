using Redis.Cache;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Geosearch : Base
{
    protected override string Name => nameof(Geosearch);
    public override bool CanBePropagated => false;
    
    protected override async Task<string> OnMasterNodeExecute(CommandContext commandContext)
    {
        return await GenerateCommonResponse(commandContext);
    }
    
    protected override async Task<string> OnReplicaNodeExecute(CommandContext commandContext)
    {
        return await GenerateCommonResponse(commandContext);
    }
    
    private static Task<string> GenerateCommonResponse(CommandContext commandContext)
    {
        var commands = commandContext.CommandDetails.CommandParts;

        var key = commands[4];
        var longitude = double.Parse(commands[8]);
        var latitude = double.Parse(commands[10]);
        var radius = double.Parse(commands[14]);
        
        var result = DataCache.Geosearch(key, longitude, latitude, radius);
        
        var resp = RespBuilder.InitArray(result.Count);
        foreach (var member in result)
        {
            resp += RespBuilder.BulkString(member);
        }

        commandContext.Socket.SendCommand(resp);
        return Task.FromResult(resp);
    }
}
