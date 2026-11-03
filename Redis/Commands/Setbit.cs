using Redis.Cache;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Setbit : Base
{
    protected override string Name => nameof(Setbit);
    public override bool CanBePropagated => true;

    // Redis caps bitmaps at 512 MB, i.e. 2^32 bits.
    private const long MaxOffset = (1L << 32) - 1;

    protected override Task<string> ExecuteCore(CommandContext commandContext)
    {
        var parts = commandContext.CommandDetails.CommandParts;
        string response;

        if (parts.Length < 9)
        {
            response = RespBuilder.Error("wrong number of arguments for 'setbit' command");
            commandContext.Socket.SendCommand(response);
            return Task.FromResult(response);
        }

        var key = parts[4];

        if (!long.TryParse(parts[6], out var offset) || offset < 0 || offset > MaxOffset)
        {
            response = RespBuilder.Error("bit offset is not an integer or out of range");
            SendIfNotFromTransaction(commandContext, response);
            return Task.FromResult(response);
        }

        if (parts[8] is not ("0" or "1"))
        {
            response = RespBuilder.Error("bit is not an integer or out of range");
            SendIfNotFromTransaction(commandContext, response);
            return Task.FromResult(response);
        }

        var original = DataCache.SetBit(key, offset, parts[8] == "1" ? 1 : 0);

        response = RespBuilder.Integer(original);
        SendIfNotFromTransaction(commandContext, response);
        return Task.FromResult(response);
    }
}
