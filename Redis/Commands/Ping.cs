using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Ping : Base
{
    protected override string Name => nameof(Ping);
    public override bool CanBePropagated => false;

    protected override Task<string> OnMasterNodeExecute(CommandContext commandContext)
    {
        var resp = commandContext.Subscriptions.Count > 0
            ? RespBuilder.ArrayFromCommands("pong", string.Empty)
            : RespBuilder.SimpleString("PONG");

        commandContext.Socket.SendCommand(resp);
        return Task.FromResult(resp);
    }
}
