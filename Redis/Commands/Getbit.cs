using Redis.Cache;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Getbit : Base
{
    protected override string Name => nameof(Getbit);
    public override bool CanBePropagated => false;

    protected override Task<string> ExecuteCore(CommandContext commandContext)
    {
        var parts = commandContext.CommandDetails.CommandParts;
        string response;

        if (parts.Length < 7)
        {
            response = RespBuilder.Error("wrong number of arguments for 'getbit' command");
            commandContext.Socket.SendCommand(response);
            return Task.FromResult(response);
        }

        if (!long.TryParse(parts[6], out var offset) || offset < 0)
        {
            response = RespBuilder.Error("bit offset is not an integer or out of range");
            SendIfNotFromTransaction(commandContext, response);
            return Task.FromResult(response);
        }

        response = RespBuilder.Integer(DataCache.GetBit(parts[4], offset));
        SendIfNotFromTransaction(commandContext, response);
        return Task.FromResult(response);
    }
}
