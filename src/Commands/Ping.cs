using System.Text;

namespace codecrafters_redis.Commands;

public class Ping : Base
{
    public override bool CanBePropagated => false;

    protected override Task<string> OnMasterNodeExecute(CommandContext commandContext)
    {
        const string response = "+PONG\r\n";
        
        if (!commandContext.ReplicaConnection)
        {
            commandContext.Socket.Send(Encoding.UTF8.GetBytes(response));
        }

        return Task.FromResult(response);
    }
}