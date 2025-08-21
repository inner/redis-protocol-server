using Redis.Cache;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Zrem : Base
{
    protected override string Name => nameof(Zrem);
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
        var member = commands[6];
        
        var result = DataCache.Zrem(key, member);
        var resp = RespBuilder.Integer(result);
        
        commandContext.Socket.SendCommand(resp);
        return Task.FromResult(resp);
    }
    
    public override Dictionary<string, Dictionary<string, string>> Docs()
    {
        return new()
        {
            {
                "ZREM",
                new()
                {
                    { "description", "Removes one or more members from a sorted set." },
                    { "syntax", "ZREM key member [member ...]" },
                    { "group", "Sorted Sets" },
                    { "complexity", "O(log(N)) for each item removed, where N is the number of elements in the sorted set." }
                }
            }
        };
    }
}