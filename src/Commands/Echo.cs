using System.Net.Sockets;
using System.Text;
using codecrafters_redis.Receivers;

namespace codecrafters_redis.Commands;

public class Echo : Base
{
    public override bool CanBePropagated => false;

    protected override Task OnMasterNodeExecute(Socket socket, CommandDetails commandDetails,
        List<CommandQueueItem> commandQueue, ReceiverBase receiver, bool replicaConnection = false)
    {
        GenerateCommonResponse(socket, commandDetails, replicaConnection);
        return Task.CompletedTask;
    }

    protected override Task OnReplicaNodeExecute(Socket socket, CommandDetails commandDetails,
        List<CommandQueueItem> commandQueue, ReceiverBase receiver, bool replicaConnection = false)
    {
        GenerateCommonResponse(socket, commandDetails, replicaConnection);
        return Task.CompletedTask;
    }

    private static void GenerateCommonResponse(Socket socket, CommandDetails commandDetails, bool replicaConnection)
    {
        var response = commandDetails.CommandCount switch
        {
            2 => $"${commandDetails.CommandParts[4].Length}\r\n{commandDetails.CommandParts[4]}\r\n",
            _ => throw new ArgumentException($"Wrong number of arguments for '{nameof(Echo)}' " +
                                             $"command: {commandDetails.CommandCount}.")
        };

        if (!replicaConnection)
        {
            socket.Send(Encoding.UTF8.GetBytes(response));
        }
    }
}