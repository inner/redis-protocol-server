using System.Collections.Concurrent;
using System.Net.Sockets;

namespace codecrafters_redis.Commands;

public class Exec : Base
{
    public override bool CanBePropagated => true;

    protected override Task OnMasterNodeExecute(Socket socket, int commandCount, string[] commandParts,
        ConcurrentQueue<string> concurrentQueue,
        bool replicaConnection = false)
    {
        GenerateCommonResponse(socket, commandParts, concurrentQueue, replicaConnection);
        return Task.CompletedTask;
    }

    protected override Task OnReplicaNodeExecute(Socket socket, int commandCount, string[] commandParts,
        ConcurrentQueue<string> concurrentQueue,
        bool replicaConnection = false)
    {
        GenerateCommonResponse(socket, commandParts, concurrentQueue, replicaConnection);
        return Task.CompletedTask;
    }
    
    private static void GenerateCommonResponse(Socket socket, string[] commandParts,
        ConcurrentQueue<string> concurrentQueue, bool replicaConnection = false)
    {
        if (!concurrentQueue.Contains(nameof(Multi)))
        {
            socket.Send("-ERR EXEC without MULTI\r\n"u8.ToArray());
            return;
        }

        socket.Send("*0\r\n"u8.ToArray());
        concurrentQueue.Clear();
    }
}