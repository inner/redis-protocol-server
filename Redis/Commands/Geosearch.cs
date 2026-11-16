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
        double? metersPerUnit = commands.Length > 16
            ? commands[16].ToLowerInvariant() switch
            {
                "m" => 1,
                "km" => 1000,
                "mi" => 1609.34,
                "ft" => 0.3048,
                _ => null
            }
            : 1;

        if (metersPerUnit is null)
        {
            var error = RespBuilder.Error("unsupported unit provided. please use M, KM, FT, MI");
            commandContext.Socket.SendCommand(error);
            return Task.FromResult(error);
        }

        radius *= metersPerUnit.Value;

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
