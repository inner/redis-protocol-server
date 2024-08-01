using System.Net.Sockets;
using System.Text;
using codecrafters_redis.Common;
using codecrafters_redis.Receivers;

namespace codecrafters_redis.Commands;

public class Multi : Base
{
    public override bool CanBePropagated => true;

    protected override async Task<string> OnMasterNodeExecute(Socket socket, CommandDetails commandDetails,
        List<CommandQueueItem> commandQueue, ReceiverBase receiver, bool replicaConnection = false)
    {
        return await GenerateCommonResponse(socket, commandQueue);
    }

    protected override async Task<string> OnReplicaNodeExecute(Socket socket, CommandDetails commandDetails,
        List<CommandQueueItem> commandQueue, ReceiverBase receiver, bool replicaConnection = false)
    {
        return await GenerateCommonResponse(socket, commandQueue);
    }

    private static Task<string> GenerateCommonResponse(Socket socket, List<CommandQueueItem> commandQueue)
    {
        if (commandQueue.All(x => x.CommandType != CommandType.Multi))
        {
            commandQueue.Add(new CommandQueueItem
            {
                CommandType = CommandType.Multi,
                CommandString = string.Empty
            });
        }

        var result = Constants.OkResponse;
        socket.Send(Encoding.UTF8.GetBytes(result));
        return Task.FromResult(result);
    }
}