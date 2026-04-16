using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Echo : Base
{
    protected override string Name => nameof(Echo);
    public override bool CanBePropagated => false;

    protected override Task<string> ExecuteCore(CommandContext commandContext)
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
