using System.Net.Sockets;
using System.Text;

namespace codecrafters_redis.Commands;

public class Multi : Base
{
    public override bool CanBePropagated => false;

    protected override Task OnMasterNodeExecute(Socket socket, int commandCount, string[] commandParts,
        bool replicaConnection = false)
    {
        GenerateCommonResponse(socket, commandParts, replicaConnection);
        return Task.CompletedTask;
    }

    protected override Task OnReplicaNodeExecute(Socket socket, int commandCount, string[] commandParts,
        bool replicaConnection = false)
    {
        GenerateCommonResponse(socket, commandParts, replicaConnection);
        return Task.CompletedTask;
    }

    private static void GenerateCommonResponse(Socket socket, string[] commandParts, bool replicaConnection = false)
    {
        socket.Send(Encoding.UTF8.GetBytes(Constants.OkResponse));
    }
}