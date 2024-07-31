using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;

namespace codecrafters_redis.Commands;

public class Config : Base
{
    public override bool CanBePropagated => false;

    protected override Task OnMasterNodeExecute(Socket socket, int commandCount, string[] commandParts,
        ConcurrentQueue<string> concurrentQueue, bool replicaConnection = false)
    {
        return GenerateCommonResponse(socket, commandParts, replicaConnection);
    }

    protected override Task OnReplicaNodeExecute(Socket socket, int commandCount, string[] commandParts,
        ConcurrentQueue<string> concurrentQueue, bool replicaConnection = false)
    {
        return GenerateCommonResponse(socket, commandParts, replicaConnection);
    }

    private static Task GenerateCommonResponse(Socket socket, string[] commandParts, bool replicaConnection = false)
    {
        if (Array.IndexOf(commandParts, "GET") != -1 && Array.IndexOf(commandParts, "dir") != -1)
        {
            if (!replicaConnection)
            {
                socket.Send(
                    Encoding.UTF8.GetBytes(
                        $"*2\r\n$3\r\ndir\r\n${ServerInfo.ServerRuntimeContext.DataDir.Length}\r\n{ServerInfo.ServerRuntimeContext.DataDir}\r\n"));
            }

            return Task.CompletedTask;
        }

        if (Array.IndexOf(commandParts, "GET") != -1 && Array.IndexOf(commandParts, "dbfilename") != -1)
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