using System.Text;
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
            result = Constants.OkResponse;
            commandContext.Socket.Send(Encoding.UTF8.GetBytes(Constants.OkResponse));
        }
        else
        {
            result = "-ERR DISCARD without MULTI\r\n";
            commandContext.Socket.Send(Encoding.UTF8.GetBytes(result));
        }

        return Task.FromResult(result);
    }
}