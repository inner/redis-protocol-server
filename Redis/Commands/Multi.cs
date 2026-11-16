using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Multi : Base
{
    protected override string Name => nameof(Multi);
    // Transactions reach replicas as one block sent by EXEC.
    public override bool CanBePropagated => false;

    protected override Task<string> ExecuteCore(CommandContext commandContext)
    {
        string result;
        if (commandContext.CommandQueue.Any(x => x.RespType == RespType.Multi))
        {
            result = RespBuilder.Error("MULTI calls can not be nested");
            Reply(commandContext, result);
            return Task.FromResult(result);
        }

        commandContext.CommandQueue.Add(
            new CommandQueueItem { RespType = RespType.Multi, Resp = string.Empty });

        result = RespBuilder.SimpleString("OK");
        Reply(commandContext, result);
        return Task.FromResult(result);
    }
}
