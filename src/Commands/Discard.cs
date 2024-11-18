using codecrafters_redis.Commands.Common;
using codecrafters_redis.Common;

namespace codecrafters_redis.Commands;

public class Discard : Base
{
    public override bool CanBePropagated => true;

    protected override async Task<string> OnMasterNodeExecute(CommandContext commandContext)
    {
        return await GenerateCommonResponse(commandContext);
    }

    protected override async Task<string> OnReplicaNodeExecute(CommandContext commandContext)
    {
        return await GenerateCommonResponse(commandContext);
    }

    private Task<string> GenerateCommonResponse(CommandContext commandContext)
    {
        string result;

        if (commandContext.CommandQueue.Count > 0)
        {
            commandContext.CommandQueue.Clear();
            result = RespBuilder.SimpleString("OK");
        }
        else
        {
            result = RespBuilder.Error("DISCARD without MULTI");
        }

        commandContext.Socket.SendCommand(result);
        return Task.FromResult(result);
    }
}