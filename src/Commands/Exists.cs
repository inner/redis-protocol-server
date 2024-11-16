using codecrafters_redis.Common;

namespace codecrafters_redis.Commands;

// spec: https://redis.io/docs/latest/commands/exists/
public class Exists : Base
{
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
        var result = RespBuilder.Integer(1);
        commandContext.Socket.Send(result.AsBytes());
        return Task.FromResult(result);
    }
}