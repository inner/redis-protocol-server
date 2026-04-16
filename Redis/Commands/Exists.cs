using Redis.Cache;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

// spec: https://redis.io/docs/latest/commands/exists/
public class Exists : Base
{
    protected override string Name => nameof(Exists);
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
        var keys = commandContext.CommandDetails.CommandParts[3..]
            .Where(x => !x.StartsWith("$"))
            .Distinct();

        var count = DataCache.CountKeys(keys.ToArray());
        var result = RespBuilder.Integer(count);
        commandContext.Socket.SendCommand(result);
        return Task.FromResult(result);
    }
}
