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
        var unit = commands[16];
        
        var result = DataCache.Geosearch(key, longitude, latitude, radius, unit);
        
        var resp = RespBuilder.InitArray(result.Count);
        foreach (var member in result)
        {
            resp += RespBuilder.BulkString(member);
        }

        commandContext.Socket.SendCommand(resp);
        return Task.FromResult(resp);
    }

    public override Dictionary<string, Dictionary<string, string>> Docs()
    {
        return new()
        {
            {
                "GEOSEARCH",
                new()
                {
                    {
                        "description",
                        "Query a sorted set representing a geospatial index to fetch members matching a given maximum distance from a point or within a bounding box."
                    },
                    {
                        "syntax",
                        "GEOSEARCH key FROMMEMBER member BYRADIUS radius m|km|ft|mi [WITHCOORD] [WITHDIST] [WITHHASH] [COUNT count [ANY]] [ASC|DESC]"
                    },
                    { "group", "Geospatial" },
                    {
                        "complexity",
                        "O(log(N)+M) with N being the number of elements in the sorted set and M the number of elements being returned."
                    }
                }
            }
        };
    }
}