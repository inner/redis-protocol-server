using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Discard : Base
{
    protected override string Name => nameof(Discard);
    public override bool CanBePropagated => true;

    protected override Task<string> ExecuteCore(CommandContext commandContext)
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
