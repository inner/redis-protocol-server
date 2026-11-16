using System.Globalization;
using Redis.Cache;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Geodist : Base
{
    protected override string Name => nameof(Geodist);
    public override bool CanBePropagated => false;

    protected override Task<string> ExecuteCore(CommandContext commandContext)
    {
        var commands = commandContext.CommandDetails.CommandParts;

        var key = commands[4];
        var location1 = commands[6];
        var location2 = commands[8];

        // GEODIST key member1 member2 [M|KM|FT|MI]; meters by default.
        var metersPerUnit = commands.Length > 10
            ? GeoCalculator.MetersPerUnit(commands[10])
            : 1;

        string resp;
        if (metersPerUnit is null)
        {
            resp = RespBuilder.Error(GeoCalculator.UnsupportedUnitError);
            Reply(commandContext, resp);
            return Task.FromResult(resp);
        }

        // A missing member (or key) yields a null reply, as in Redis.
        var result = DataCache.Geodist(key, location1, location2);
        resp = result == null
            ? RespBuilder.Null()
            : RespBuilder.BulkString(
                (result.Value / metersPerUnit.Value).ToString("F4", CultureInfo.InvariantCulture));

        Reply(commandContext, resp);
        return Task.FromResult(resp);
    }
}
