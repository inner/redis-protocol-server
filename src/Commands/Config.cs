using System.Net.Sockets;
using System.Text;
using codecrafters_redis.Receivers;

namespace codecrafters_redis.Commands;

public class Config : Base
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
        bool replicaConnection = false)
    {
        var result =
            $"*2\r\n$3\r\ndir\r\n${ServerInfo.ServerRuntimeContext.DataDir.Length}\r\n{ServerInfo.ServerRuntimeContext.DataDir}\r\n";
        
        if (Array.IndexOf(commandDetails.CommandParts, "GET") != -1 &&
            Array.IndexOf(commandDetails.CommandParts, "dir") != -1)
        {
            if (!replicaConnection)
            {
                socket.Send(Encoding.UTF8.GetBytes(result));
            }

            return Task.FromResult(result);
        }

        if (Array.IndexOf(commandDetails.CommandParts, "GET") != -1 &&
            Array.IndexOf(commandDetails.CommandParts, "dbfilename") != -1)
        {
            result =
                $"*2\r\n$10\r\ndbfilename\r\n${ServerInfo.ServerRuntimeContext.DbFilename.Length}\r\n{ServerInfo.ServerRuntimeContext.DbFilename}\r\n";
            
            if (!replicaConnection)
            {
                socket.Send(
                    Encoding.UTF8.GetBytes(result));
            }

            return Task.FromResult(result);
        }

        result = "-ERR Unsupported CONFIG parameter\r\n";
        
        if (!replicaConnection)
        {
            socket.Send(Encoding.UTF8.GetBytes(result));
        }

        return Task.FromResult(result);
    }
}