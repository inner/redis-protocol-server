using codecrafters_redis.Common;

namespace codecrafters_redis.Commands;

public class Client : Base
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
        var result = RespBuilder.SimpleString("OK");
        
        if (!commandContext.ReplicaConnection)
        {
            commandContext.Socket.Send(result.AsBytes());
        }

        return Task.FromResult(result);
    }
}