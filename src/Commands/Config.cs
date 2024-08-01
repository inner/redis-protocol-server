using System.Net.Sockets;
using System.Text;
using codecrafters_redis.Receivers;

namespace codecrafters_redis.Commands;

public class Config : Base
{
    public override bool CanBePropagated => false;

    protected override Task OnMasterNodeExecute(Socket socket, CommandDetails commandDetails,
        List<CommandQueueItem> commandQueue, ReceiverBase receiver, bool replicaConnection = false)
    {
        return GenerateCommonResponse(socket, commandDetails, replicaConnection);
    }

    protected override Task OnReplicaNodeExecute(Socket socket, CommandDetails commandDetails,
        List<CommandQueueItem> commandQueue, ReceiverBase receiver, bool replicaConnection = false)
    {
        return GenerateCommonResponse(socket, commandDetails, replicaConnection);
    }

    private static Task GenerateCommonResponse(Socket socket, CommandDetails commandDetails,
        bool replicaConnection = false)
    {
        if (Array.IndexOf(commandDetails.CommandParts, "GET") != -1 &&
            Array.IndexOf(commandDetails.CommandParts, "dir") != -1)
        {
            if (!replicaConnection)
            {
                socket.Send(
                    Encoding.UTF8.GetBytes(
                        $"*2\r\n$3\r\ndir\r\n${ServerInfo.ServerRuntimeContext.DataDir.Length}\r\n{ServerInfo.ServerRuntimeContext.DataDir}\r\n"));
            }

            return Task.CompletedTask;
        }

        if (Array.IndexOf(commandDetails.CommandParts, "GET") != -1 &&
            Array.IndexOf(commandDetails.CommandParts, "dbfilename") != -1)
        {
            if (!replicaConnection)
            {
                socket.Send(
                    Encoding.UTF8.GetBytes(
                        $"*2\r\n$10\r\ndbfilename\r\n${ServerInfo.ServerRuntimeContext.DbFilename.Length}\r\n{ServerInfo.ServerRuntimeContext.DbFilename}\r\n"));
            }

            return Task.CompletedTask;
        }

        if (!replicaConnection)
        {
            socket.Send(Encoding.UTF8.GetBytes("-ERR Unsupported CONFIG parameter\r\n"));
        }

        return Task.CompletedTask;
    }
}