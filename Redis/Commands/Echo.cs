using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Echo : Base
{
    protected override string Name => nameof(Echo);
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
        var response = commandContext.CommandDetails.CommandCount switch
        {
            2 => RespBuilder.BulkString(commandContext.CommandDetails.CommandParts[4]),
            _ => RespBuilder.Error("Wrong number of arguments")
        };

        commandContext.Socket.SendCommand(response);
        return Task.FromResult(response);
    }
}
