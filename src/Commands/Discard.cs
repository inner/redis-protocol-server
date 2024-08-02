using System.Net.Sockets;
using System.Text;
using codecrafters_redis.Common;
using codecrafters_redis.Receivers;

namespace codecrafters_redis.Commands;

public class Discard : Base
{
    public override bool CanBePropagated => true;

    protected override async Task<string> OnMasterNodeExecute(Socket socket, CommandDetails commandDetails,
        List<CommandQueueItem> commandQueue, ReceiverBase receiver,
        bool replicaConnection = false)
    {
        return await GenerateCommonResponse(socket, commandQueue);
    }

    protected override async Task<string> OnReplicaNodeExecute(Socket socket, CommandDetails commandDetails,
        List<CommandQueueItem> commandQueue, ReceiverBase receiver,
        bool replicaConnection = false)
    {
        return await GenerateCommonResponse(socket, commandQueue);
    }

    private Task<string> GenerateCommonResponse(Socket socket, List<CommandQueueItem> commandQueue)
    {
        string result;

        if (commandQueue.Count > 0)
        {
            commandQueue.Clear();
            result = Constants.OkResponse;
            socket.Send(Encoding.UTF8.GetBytes(Constants.OkResponse));
        }
        else
        {
            result = "-ERR DISCARD without MULTI\r\n";
            socket.Send(Encoding.UTF8.GetBytes(result));
        }

        return Task.FromResult(result);
    }
}