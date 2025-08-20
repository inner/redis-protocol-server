using Redis.Cache;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Zadd : Base
{
    protected override string Name => nameof(Zadd);
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
        var score = commands[6];
        var member = commands[8];
        
        var result = DataCache.Zadd(key, double.Parse(score), member);
        var resp = RespBuilder.Integer(result);
        
        commandContext.Socket.SendCommand(resp);
        return Task.FromResult(resp);
    }

    public override Dictionary<string, Dictionary<string, string>> Docs()
    {
        return new()
        {
            {
                "ZADD",
                new()
                {
                    { "description", "Adds one or more members to a sorted set, or updates the score of an existing member." },
                    { "syntax", "ZADD key [NX|XX] [CH] [INCR] score member [score member ...]" },
                    { "group", "Sorted Sets" },
                    { "complexity", "O(log(N)) for each item added, where N is the number of elements in the sorted set." }
                }
            }
        };
    }
}