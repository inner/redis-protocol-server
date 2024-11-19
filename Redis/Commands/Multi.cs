using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Multi : Base
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

    private static Task<string> GenerateCommonResponse(CommandContext commandContext)
    {
        if (commandContext.CommandQueue.All(x => x.RespType != RespType.Multi))
        {
            commandContext.CommandQueue.Add(new CommandQueueItem
            {
                RespType = RespType.Multi,
                Resp = string.Empty
            });
        }

        var result = RespBuilder.SimpleString("OK");
        commandContext.Socket.SendCommand(result);
        return Task.FromResult(result);
    }
}