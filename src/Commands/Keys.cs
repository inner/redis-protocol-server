using System.Net.Sockets;
using System.Text;
using codecrafters_redis.Cache;
using codecrafters_redis.Receivers;

namespace codecrafters_redis.Commands;

public class Keys : Base
{
    public override bool CanBePropagated => false;

    protected override async Task<string> OnMasterNodeExecute(Socket socket, CommandDetails commandDetails,
        List<CommandQueueItem> commandQueue, ReceiverBase receiver, bool replicaConnection = false)
    {
        return await GenerateCommonResponse(socket, commandDetails, replicaConnection);
    }

    protected override async Task<string> OnReplicaNodeExecute(Socket socket, CommandDetails commandDetails,
        List<CommandQueueItem> commandQueue, ReceiverBase receiver, bool replicaConnection = false)
    {
        return await GenerateCommonResponse(socket, commandDetails, replicaConnection);
    }

    private static Task<string> GenerateCommonResponse(Socket socket, CommandDetails commandDetails,
        bool replicaConnection)
    {
        string result;
        var keys = DataCache.GetKeys(commandDetails.CommandParts[4]);
        if (keys.Count == 0)
        {
            result = "*0";
            socket.Send(Encoding.UTF8.GetBytes(result));
        }

        var sb = new StringBuilder();
        sb.Append($"*{keys.Count}\r\n");

        foreach (var key in keys)
        {
            sb.Append($"${key.Length}\r\n{key}\r\n");
        }

        result = sb.ToString();

        if (!replicaConnection)
        {
            socket.Send(Encoding.UTF8.GetBytes(result));
        }

        return Task.FromResult(result);
    }
}