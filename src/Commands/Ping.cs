using System.Net.Sockets;
using System.Text;
using codecrafters_redis.Receivers;

namespace codecrafters_redis.Commands;

public class Ping : Base
{
    public override bool CanBePropagated => false;

    protected override async Task<string> OnMasterNodeExecute(Socket socket, CommandDetails commandDetails,
        List<CommandQueueItem> commandQueue, ReceiverBase receiver, bool replicaConnection = false)
    {
        return await GenerateCommonResponse(socket, replicaConnection);
    }

    protected override Task<string> OnReplicaNodeExecute(Socket socket, CommandDetails commandDetails,
        List<CommandQueueItem> commandQueue, ReceiverBase receiver, bool replicaConnection = false)
    {
        // if (replicaConnection)
        // {
        //     return Task.CompletedTask;
        // }
        //
        
        const string response = "*1\r\n$4\r\nPONG\r\n";
        socket.Send(Encoding.UTF8.GetBytes(response));
        return Task.FromResult(string.Empty);
    }

    private static Task<string> GenerateCommonResponse(Socket socket, bool replicaConnection)
    {
        const string response = "+PONG\r\n";
        
        if (!replicaConnection)
        {
            socket.Send(Encoding.UTF8.GetBytes(response));
        }

        return Task.FromResult(response);
    }
}