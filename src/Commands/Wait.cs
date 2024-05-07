using System.Net.Sockets;
using System.Text;

namespace codecrafters_redis.Commands;

public class Wait : Base
{
    public override bool CanBePropagated => false;

    protected override void OnMasterNodeExecute(Socket socket, int commandCount, string[] commandParts,
        bool replicaConnection = false)
    {
        socket.Send(Encoding.UTF8.GetBytes($":{ServerInfo.ConnectedReplicas}\r\n"));
    }

    protected override void OnReplicaNodeExecute(Socket socket, int commandCount, string[] commandParts,
        bool replicaConnection = false)
    { }
}