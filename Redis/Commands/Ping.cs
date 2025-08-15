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

        if (!commandContext.ReplicaConnection)
        {
            commandContext.Socket.SendCommand(resp);
        }

        return Task.FromResult(resp);
    }

    public override Dictionary<string, Dictionary<string, string>> Docs()
    {
        return new()
        {
            {
                Name,
                new()
                {
                    { "summary", "Returns the server's liveliness response." },
                    { "usage", "redis-cli PING" }
                }
            }
        };
    }
}