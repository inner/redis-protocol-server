using System.Net.Sockets;
using System.Text;

namespace codecrafters_redis.Commands;

public class Client : Base
{
    public override bool CanBePropagated => false;

    protected override void OnMasterNodeExecute(Socket socket, int commandCount, string[] commandParts, int bytesReceived,
        bool replicaConnection = false)
    {
        GenerateCommonResponse(socket, replicaConnection);
    }

    protected override void OnReplicaNodeExecute(Socket socket, int commandCount, string[] commandParts, int bytesReceived,
        bool replicaConnection = false)
    {
        GenerateCommonResponse(socket, replicaConnection);
    }

    private static void GenerateCommonResponse(Socket socket, bool replicaConnection)
    {
        if (!replicaConnection)
        {
            socket.Send(Encoding.UTF8.GetBytes(Constants.OkResponse));
        }
    }
}