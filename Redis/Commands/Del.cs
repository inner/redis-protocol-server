using Redis.Cache;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Del : Base
{
    protected override string Name => nameof(Del);
    public override bool CanBePropagated => true;

    protected override Task<string> OnMasterNodeExecute(CommandContext commandContext)
    {
        return GenerateCommonResponse(commandContext);
    }
    
    protected override Task<string> OnReplicaNodeExecute(CommandContext commandContext)
    {
        return GenerateCommonResponse(commandContext);
    }

    private static Task<string> GenerateCommonResponse(CommandContext commandContext)
    {
        var keys = commandContext.CommandDetails.CommandParts[3..]
            .Where(x => !x.StartsWith("$"))
            .Distinct();

        var count = DataCache.DelKeys(keys.ToArray());
        var result = RespBuilder.Integer(count);
        commandContext.Socket.SendCommand(result);
        return Task.FromResult(result);
    }
    
    public override Dictionary<string, Dictionary<string, string>> Docs()
    {
        return new()
        {
            {
                Name,
                new()
                {
                    { "summary", "Deletes one or more keys." },
                    { "usage #1", "redis-cli SET key1 val1" },
                    { "usage #2", "redis-cli SET key2 val2" },
                    { "usage #3", "redis-cli DEL key1 key2 key3" }
                }
            }
        };
    }
}