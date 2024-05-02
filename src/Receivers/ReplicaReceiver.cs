using System.Net.Sockets;
using System.Text;

namespace codecrafters_redis.Receivers;

public class ReplicaReceiver : ReceiverBase
{
    public override void Receive(Socket socket, string commandString)
    {
        var currentBytesRead = Encoding.UTF8.GetBytes(commandString).Length;
        base.Receive(socket, commandString);

        if (ServerInfo.ReplicaHandshakeCompleted)
        {
            ServerInfo.BytesRead += currentBytesRead;
        }
    }
}