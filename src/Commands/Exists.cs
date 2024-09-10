using System.Net.Sockets;
using System.Text;
using codecrafters_redis.Receivers;

namespace codecrafters_redis.Commands;

public class Exists : Base
{
    public override bool CanBePropagated => false;

    protected override async Task<string> OnMasterNodeExecute(Socket socket, CommandDetails commandDetails,
        List<CommandQueueItem> commandQueue, ReceiverBase receiver, bool replicaConnection = false)
    {
        return await GenerateCommonResponse(socket, commandDetails, receiver);
    }

    protected override async Task<string> OnReplicaNodeExecute(Socket socket, CommandDetails commandDetails,
        List<CommandQueueItem> commandQueue, ReceiverBase receiver, bool replicaConnection = false)
    {
        return await GenerateCommonResponse(socket, commandDetails, receiver);
    }

    private static Task<string> GenerateCommonResponse(Socket socket, CommandDetails commandDetails,
        ReceiverBase receiver)
    {
        if (commandDetails.CommandParts.Length < 2)
        {
            throw new ArgumentException(
                $"Wrong number of arguments for '{nameof(Exists)}' command: {commandDetails.CommandParts.Length}.");
        }

        var exists = 1;
        var response = $":{exists}\r\n";

        socket.Send(Encoding.UTF8.GetBytes(response));
        return Task.FromResult(response);
    }
}