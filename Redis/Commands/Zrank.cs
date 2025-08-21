using Redis.Cache;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Zrank : Base
{
    protected override string Name => nameof(Zrank);
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
        var member = commands[6];
        
        var result = DataCache.Zrank(key, member);
        
        var resp = result >= 0
            ? RespBuilder.Integer(result)
            : RespBuilder.Null();
        
        commandContext.Socket.SendCommand(resp);
        return Task.FromResult(resp);
    }

    public override Dictionary<string, Dictionary<string, string>> Docs()
    {
        return new()
        {
            {
                "ZRANK",
                new()
                {
                    { "description", "Returns the rank (or index) of a member in a sorted set." },
                    { "syntax", "ZRANK key member" },
                    { "group", "Sorted Sets" },
                    { "complexity", "O(log(N)) where N is the number of elements in the sorted set." }
                }
            }
        };
    }
}