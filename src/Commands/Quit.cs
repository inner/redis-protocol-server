using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;

namespace codecrafters_redis.Commands;

public class Quit : Base
{
    public override bool CanBePropagated => false;

    protected override Task OnMasterNodeExecute(Socket socket, int commandCount, string[] commandParts,
        ConcurrentQueue<string> concurrentQueue, bool replicaConnection = false)
    {
        GenerateCommonResponse(socket, replicaConnection);
        return Task.CompletedTask;
    }

    protected override Task OnReplicaNodeExecute(Socket socket, int commandCount, string[] commandParts,
        ConcurrentQueue<string> concurrentQueue, bool replicaConnection = false)
    {
        GenerateCommonResponse(socket, replicaConnection);
        return Task.CompletedTask;
    }

    private static void GenerateCommonResponse(Socket socket, bool replicaConnection)
    {
        if (!replicaConnection)
        {
            socket.Send(Encoding.UTF8.GetBytes(Constants.OkResponse));
        }
    }
}