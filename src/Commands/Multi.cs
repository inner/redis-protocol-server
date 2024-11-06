using System.Text;
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

        var result = Constants.OkResponse;
        commandContext.Socket.Send(Encoding.UTF8.GetBytes(result));
        return Task.FromResult(result);
    }
}