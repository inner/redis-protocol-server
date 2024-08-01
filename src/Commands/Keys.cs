using System.Net.Sockets;
using System.Text;
using codecrafters_redis.Cache;
using codecrafters_redis.Receivers;

namespace codecrafters_redis.Commands;

public class Keys : Base
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
        var keys = DataCache.GetKeys(commandDetails.CommandParts[4]);
        if (keys.Count == 0)
        {
            socket.Send("*0"u8.ToArray());
        }
        
        var sb = new StringBuilder();
        sb.Append($"*{keys.Count}\r\n");
        
        foreach (var key in keys)
        {
            sb.Append($"${key.Length}\r\n{key}\r\n");
        }

        var response = sb.ToString();

        if (!replicaConnection)
        {
            socket.Send(Encoding.UTF8.GetBytes(response));
        }
    }
}