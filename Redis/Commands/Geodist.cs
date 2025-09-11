using System.Globalization;
using Redis.Cache;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Geodist : Base
{
    protected override string Name => nameof(Geodist);
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
        var location1 = commands[6];
        var location2 = commands[8];

        var result = DataCache.Geodist(key, location1, location2);
        
        var resp = RespBuilder.BulkString(
            result!.Value.ToString("F4", CultureInfo.InvariantCulture));

        commandContext.Socket.SendCommand(resp);
        return Task.FromResult(resp);
    }

    public override Dictionary<string, Dictionary<string, string>> Docs()
    {
        return new()
        {
            {
                "GEODIST",
                new()
                {
                    { "description", "Returns the distance between two members of a geospatial index." },
                    { "syntax", "GEODIST key member1 member2 [unit]" },
                    { "group", "Geospatial" },
                    { "complexity", "O(log(N)) where N is the number of elements in the sorted set." }
                }
            }
        };
    }
}