using Redis.Cache;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Strlen : Base
{
    protected override string Name => nameof(Strlen);
    public override bool CanBePropagated => false;

    protected override Task<string> ExecuteCore(CommandContext commandContext)
    {
        var parts = commandContext.CommandDetails.CommandParts;
        string response;

        if (parts.Length < 5)
        {
            response = RespBuilder.Error("wrong number of arguments for 'strlen' command");
            commandContext.Socket.SendCommand(response);
            return Task.FromResult(response);
        }

        // Bitmaps hold one byte per char, so Length is the byte length for
        // them. TODO: values written by SET are decoded UTF-8 text, so their
        // Length counts chars, not bytes, when they contain non-ASCII data.
        var length = DataCache.Get(parts[4])?.Value.Length ?? 0;

        response = RespBuilder.Integer(length);
        SendIfNotFromTransaction(commandContext, response);
        return Task.FromResult(response);
    }
}
