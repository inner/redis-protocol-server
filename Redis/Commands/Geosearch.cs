using Redis.Cache;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Geosearch : Base
{
    protected override string Name => nameof(Geosearch);
    public override bool CanBePropagated => false;
    
    protected override Task<string> ExecuteCore(CommandContext commandContext)
    {
        var commands = commandContext.CommandDetails.CommandParts;

        var key = commands[4];
        var longitude = double.Parse(commands[8]);
        var latitude = double.Parse(commands[10]);
        var radius = double.Parse(commands[14]);

        // Distances are computed in meters, so the radius is converted from
        // its unit first. Factors match the ones Redis uses.
        // TODO: only FROMLONLAT + BYRADIUS is supported (no FROMMEMBER/BYBOX).
        var metersPerUnit = commands.Length > 16
            ? GeoCalculator.MetersPerUnit(commands[16])
            : 1;

        if (metersPerUnit is null)
        {
            var error = RespBuilder.Error(GeoCalculator.UnsupportedUnitError);
            Reply(commandContext, error);
            return Task.FromResult(error);
        }

        radius *= metersPerUnit.Value;

        var result = DataCache.Geosearch(key, longitude, latitude, radius);
        
        var resp = RespBuilder.InitArray(result.Count);
        foreach (var member in result)
        {
            resp += RespBuilder.BulkString(member);
        }

        Reply(commandContext, resp);
        return Task.FromResult(resp);
    }
}
