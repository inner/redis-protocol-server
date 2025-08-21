using System.Text;
using Redis.Cache;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Zrange : Base
{
    protected override string Name => nameof(Zrange);
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
        var start = int.Parse(commands[6]);
        var stop = int.Parse(commands[8]);
        
        var result = DataCache.Zrange(key, start, stop);

        string resp;
        
        if (result.Count == 0)
        {
            resp = RespBuilder.EmptyArray();
        }
        else
        {
            var sb = new StringBuilder(RespBuilder.InitArray(result.Count));
            foreach (var item in result)
            {
                sb.Append(RespBuilder.BulkString(item));
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
                "ZRANGE",
                new()
                {
                    { "description", "Returns a range of members in a sorted set, by index." },
                    { "syntax", "ZRANGE key start stop [WITHSCORES]" },
                    { "group", "Sorted Sets" },
                    { "complexity", "O(log(N) + M) where N is the number of elements in the sorted set and M is the number of elements returned." }
                }
            }
        };
    }
}