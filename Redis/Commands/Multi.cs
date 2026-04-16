using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Multi : Base
{
    protected override string Name => nameof(Multi);
    public override bool CanBePropagated => true;

    protected override Task<string> ExecuteCore(CommandContext commandContext)
    {
        if (commandContext.CommandQueue.All(x => x.RespType != RespType.Multi))
        {
            commandContext.CommandQueue.Add(
                new CommandQueueItem { RespType = RespType.Multi, Resp = string.Empty });
        }

        var result = RespBuilder.SimpleString("OK");
        commandContext.Socket.SendCommand(result);
        return Task.FromResult(result);
    }
}
