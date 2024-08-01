using System.Net.Sockets;
using System.Text;
using codecrafters_redis.Common;
using codecrafters_redis.Receivers;

namespace codecrafters_redis.Commands;

public class Client : Base
{
    public override bool CanBePropagated => false;

    protected override async Task<string> OnMasterNodeExecute(Socket socket, CommandDetails commandDetails,
        List<CommandQueueItem> commandQueue, ReceiverBase receiver, bool replicaConnection = false)
    {
        return await GenerateCommonResponse(socket, replicaConnection);
    }

    protected override async Task<string> OnReplicaNodeExecute(Socket socket, CommandDetails commandDetails,
        List<CommandQueueItem> commandQueue, ReceiverBase receiver, bool replicaConnection = false)
    {
        return await GenerateCommonResponse(socket, replicaConnection);
    }

    private static Task<string> GenerateCommonResponse(Socket socket, bool replicaConnection)
    {
        var result = Constants.OkResponse;
        
        if (!replicaConnection)
        {
            socket.Send(Encoding.UTF8.GetBytes(result));
        }

        return Task.FromResult(result);
    }
}