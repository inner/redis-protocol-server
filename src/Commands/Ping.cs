using System.Net.Sockets;
using System.Text;

namespace codecrafters_redis.Commands;

public class Ping : Base
{
    public override bool CanBePropagated => true;

    protected override void OnMasterNodeExecute(Socket socket, int commandCount, string[] commandParts,
        bool replicaConnection = false)
    {
        const string response = "+PONG\r\n";
        socket.Send(Encoding.UTF8.GetBytes(response));
    }

    protected override void OnReplicaNodeExecute(Socket socket, int commandCount, string[] commandParts,
        bool replicaConnection = false)
    {
        // if (!replicaConnection)
        // {
        //     return;
        // }
        //
        // const string response = "*1\r\n$4\r\nPONG\r\n";
        // socket.Send(Encoding.UTF8.GetBytes(response));
    }
}