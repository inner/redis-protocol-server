using Redis.Cache;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Unwatch : Base
{
    protected override string Name => nameof(Unwatch);
    public override bool CanBePropagated => false;

    // Inside MULTI this is queued like any other command (Base handles that),
    // matching Redis, where a queued UNWATCH runs as part of EXEC.
    protected override Task<string> ExecuteCore(CommandContext commandContext)
    {
        WatchRegistry.Unwatch(commandContext.Socket);

        var response = RespBuilder.SimpleString("OK");
        SendIfNotFromTransaction(commandContext, response);
        return Task.FromResult(response);
    }
}
