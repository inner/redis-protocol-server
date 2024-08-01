using System.Net.Sockets;
using System.Text;
using codecrafters_redis.Receivers;

namespace codecrafters_redis.Commands;

public class Ping : Base
{
    public override bool CanBePropagated => false;

    protected override Task OnMasterNodeExecute(Socket socket, CommandDetails commandDetails,
        List<CommandQueueItem> commandQueue, ReceiverBase receiver, bool replicaConnection = false)
    {
        const string response = "+PONG\r\n";
        socket.Send(Encoding.UTF8.GetBytes(response));
        return Task.CompletedTask;
    }

    protected override Task OnReplicaNodeExecute(Socket socket, CommandDetails commandDetails,
        List<CommandQueueItem> commandQueue, ReceiverBase receiver, bool replicaConnection = false)
    {
        // if (replicaConnection)
        // {
        //     return Task.CompletedTask;
        // }
        //
        // const string response = "*1\r\n$4\r\nPONG\r\n";
        // socket.Send(Encoding.UTF8.GetBytes(response));
        return Task.CompletedTask;
    }
}