using System.Globalization;
using System.Text;
using Redis.Cache;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Geopos : Base
{
    protected override string Name => nameof(Geopos);
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
        
        var members = commands.Skip(6)
            .Where((_, i) => i % 2 == 0)
            .ToArray();

        var result = DataCache.Geopos(key, members);
        string resp;
        
        if (result == null)
        {
            var sb = new StringBuilder(RespBuilder.InitArray(members.Length));
            foreach (var _ in members)
            {
                sb.Append(RespBuilder.NullArray());
            }
            resp = sb.ToString();
        }
        else
        {
            var sb = new StringBuilder(RespBuilder.InitArray(result.Count));
            foreach (var position in result)
            {
                if (double.IsNaN(position.Value))
                {
                    sb.Append(RespBuilder.NullArray());
                }
                else
                {
                    var coordinates = GeohashDecoder.Decode((long)position.Value);
                    sb.Append(RespBuilder.InitArray(2));
                    sb.Append(RespBuilder.BulkString(coordinates.longitude.ToString(CultureInfo.InvariantCulture)));
                    sb.Append(RespBuilder.BulkString(coordinates.latitude.ToString(CultureInfo.InvariantCulture)));   
                }
            }

            resp = sb.ToString();
        }
        
        commandContext.Socket.SendCommand(resp);
        return Task.FromResult(resp);
    }

    public override Dictionary<string, Dictionary<string, string>> Docs()
    {
        return new()
        {
            {
                "GEOPOS",
                new()
                {
                    {
                        "description",
                        "Returns the positions (longitude and latitude) of members of a geospatial index."
                    },
                    { "syntax", "GEOPOS key member [member ...]" },
                    { "group", "Geospatial" },
                    { "complexity", "O(N) where N is the number of members to retrieve positions for." }
                }
            }
        };
    }
}