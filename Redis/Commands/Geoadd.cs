using Redis.Cache;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Geoadd : Base
{
    protected override string Name => nameof(Geoadd);
    public override bool CanBePropagated => true;
    
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
        var longitude = commands[6];
        var latitude = commands[8];
        var member = commands[10];
        
        var result = DataCache.Geoadd(key, double.Parse(longitude), double.Parse(latitude), member);
        var resp = RespBuilder.Integer(result);
        
        commandContext.Socket.SendCommand(resp);
        return Task.FromResult(resp);
    }
    
    public override Dictionary<string, Dictionary<string, string>> Docs()
    {
        return new()
        {
            {
                "GEOADD",
                new()
                {
                    { "description", "Adds one or more geospatial items (latitude, longitude, name) to a geospatial index represented using a sorted set." },
                    { "syntax", "GEOADD key longitude latitude member [longitude latitude member ...]" },
                    { "group", "Geospatial" },
                    { "complexity", "O(log(N)) for each item added, where N is the number of elements in the sorted set." }
                }
            }
        };
    }
}