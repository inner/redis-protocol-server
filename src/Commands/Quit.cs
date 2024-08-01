using System.Net.Sockets;
using System.Text;
using codecrafters_redis.Common;
using codecrafters_redis.Receivers;

namespace codecrafters_redis.Commands;

public class Quit : Base
{
    public override bool CanBePropagated => false;

    protected override Task OnMasterNodeExecute(Socket socket, CommandDetails commandDetails,
        List<CommandQueueItem> commandQueue, ReceiverBase receiver, bool replicaConnection = false)
    {
        GenerateCommonResponse(socket, replicaConnection);
        return Task.CompletedTask;
    }

    protected override Task OnReplicaNodeExecute(Socket socket, CommandDetails commandDetails,
        List<CommandQueueItem> commandQueue, ReceiverBase receiver, bool replicaConnection = false)
    {
        GenerateCommonResponse(socket, replicaConnection);
        return Task.CompletedTask;
    }

    private static void GenerateCommonResponse(Socket socket, bool replicaConnection)
    {
        if (!replicaConnection)
        {
            socket.Send(Encoding.UTF8.GetBytes(Constants.OkResponse));
        }
    }
}