using Redis.Cache;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Zscore : Base
{
    protected override string Name => nameof(Zscore);
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
        
        var result = DataCache.Zscore(key, member);
        
        var resp = result != null 
            ? RespBuilder.BulkString(result.ToString())
            : RespBuilder.Null();
        
        commandContext.Socket.SendCommand(resp);
        return Task.FromResult(resp);
    }

    public override Dictionary<string, Dictionary<string, string>> Docs()
    {
        return new()
        {
            {
                "ZSCORE",
                new()
                {
                    { "description", "Returns the score of a member in a sorted set." },
                    { "syntax", "ZSCORE key member" },
                    { "group", "Sorted Sets" },
                    { "complexity", "O(log(N)) where N is the number of elements in the sorted set." }
                }
            }
        };
    }
}