using System.Net.Sockets;
using System.Text;

namespace codecrafters_redis.Commands;

public class Client : Base
{
    public override bool CanBePropagated => false;

    public override void Execute(Socket socket, int commandCount, string[] commandParts, int bytesReceived,
        bool replicaConnection = false)
    {
        if (!replicaConnection)
        {
            socket.Send(Encoding.UTF8.GetBytes(Constants.OkResponse));
        }
    }
}