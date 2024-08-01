using System.Net.Sockets;
using System.Text;
using codecrafters_redis.Enums;
using codecrafters_redis.Receivers;

namespace codecrafters_redis.Commands;

public class Multi : Base
{
    public override bool CanBePropagated => true;

    protected override Task OnMasterNodeExecute(Socket socket, int commandCount, string[] commandParts,
        List<CommandQueueItem> commandQueue, ReceiverBase receiver, bool replicaConnection = false)
    {
        GenerateCommonResponse(socket, commandQueue);
        return Task.CompletedTask;
    }

    protected override Task OnReplicaNodeExecute(Socket socket, int commandCount, string[] commandParts,
        List<CommandQueueItem> commandQueue, ReceiverBase receiver, bool replicaConnection = false)
    {
        GenerateCommonResponse(socket, commandQueue);
        return Task.CompletedTask;
    }

    private static void GenerateCommonResponse(Socket socket, List<CommandQueueItem> commandQueue)
    {
        if (commandQueue.All(x => x.CommandType != CommandType.Multi))
        {
            commandQueue.Add(new CommandQueueItem
            {
                CommandType = CommandType.Multi,
                CommandString = string.Empty
            });
        }

        socket.Send(Encoding.UTF8.GetBytes(Constants.OkResponse));
    }
}