using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Ping : Base
{
    public override bool CanBePropagated => false;

    public override Dictionary<string, Dictionary<string, string>> Docs()
    {
        return new()
        {
            {
                "PING",
                new()
                {
                    { "summary", "Returns the server's liveliness response." },
                    { "usage", "redis-cli PING" }
                }
            }
        };
    }

    protected override Task<string> OnMasterNodeExecute(CommandContext commandContext)
    {
        var pong = RespBuilder.SimpleString("PONG");
        
        if (!commandContext.ReplicaConnection)
        {
            commandContext.Socket.SendCommand(pong);
        }

        return Task.FromResult(pong);
    }
}