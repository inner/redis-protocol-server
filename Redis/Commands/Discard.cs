using Redis.Cache;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Discard : Base
{
    protected override string Name => nameof(Discard);
    // Transactions reach replicas as one block sent by EXEC.
    public override bool CanBePropagated => false;

    protected override Task<string> ExecuteCore(CommandContext commandContext)
    {
        string result;

        if (commandContext.CommandQueue.Count > 0)
        {
            commandContext.CommandQueue.Clear();
            // Like Redis, DISCARD also drops the connection's watched keys.
            WatchRegistry.Unwatch(commandContext.Socket);
            result = RespBuilder.SimpleString("OK");
        }
        else
        {
            result = RespBuilder.Error("DISCARD without MULTI");
        }

        Reply(commandContext, result);
        return Task.FromResult(result);
    }
}
