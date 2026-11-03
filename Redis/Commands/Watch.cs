using Redis.Cache;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Watch : Base
{
    protected override string Name => nameof(Watch);
    public override bool CanBePropagated => false;

    protected override Task<string> ExecuteCore(CommandContext commandContext)
    {
        string response;

        // WATCH must precede MULTI: once commands are being queued, the
        // snapshot a WATCH would take is already too late to be meaningful.
        // The MULTI marker in the queue is how this codebase tracks "inside a
        // transaction" per connection.
        if (commandContext.CommandQueue.Any(x => x.RespType == RespType.Multi))
        {
            response = RespBuilder.Error("WATCH inside MULTI is not allowed");
            commandContext.Socket.SendCommand(response);
            return Task.FromResult(response);
        }

        // RESP array parts alternate between length headers and values, so the
        // keys sit at every other index starting after the command name.
        var parts = commandContext.CommandDetails.CommandParts;
        for (var i = 4; i < parts.Length; i += 2)
        {
            if (!string.IsNullOrEmpty(parts[i]))
            {
                WatchRegistry.Watch(commandContext.Socket, parts[i]);
            }
        }

        response = RespBuilder.SimpleString("OK");
        SendIfNotFromTransaction(commandContext, response);
        return Task.FromResult(response);
    }
}
