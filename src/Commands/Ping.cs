using codecrafters_redis.Commands.Common;
using codecrafters_redis.Common;

namespace codecrafters_redis.Commands;

public class Ping : Base
{
    public override bool CanBePropagated => false;

    protected override Task<string> OnMasterNodeExecute(CommandContext commandContext)
    {
        var response = RespBuilder.SimpleString("PONG");
        
        if (!commandContext.ReplicaConnection)
        {
            commandContext.Socket.Send(response.AsBytes());
        }

        return Task.FromResult(response);
    }
}