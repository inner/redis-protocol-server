using codecrafters_redis.Commands.Common;
using codecrafters_redis.Common;

namespace codecrafters_redis.Commands;

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
        if (commandContext.CommandQueue.All(x => x.CommandType != CommandType.Multi))
        {
            commandContext.CommandQueue.Add(new CommandQueueItem
            {
                CommandType = CommandType.Multi,
                CommandString = string.Empty
            });
        }

        var result = RespBuilder.SimpleString("OK");
        commandContext.Socket.Send(result.AsBytes());
        return Task.FromResult(result);
    }
}