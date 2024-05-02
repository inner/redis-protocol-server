using System.Net.Sockets;
using System.Text;

namespace codecrafters_redis.Receivers;

public class ReplicaReceiver : ReceiverBase
{
    public override void Receive(Socket socket, string commandString)
    {
        if (!ServerInfo.ReplicaHandshakeCompleted)
        {
            return;
        }
        
        base.Receive(socket, commandString);
        
        var currentBytesRead = Encoding.UTF8.GetBytes(commandString).Length;
        ServerInfo.BytesReceived += currentBytesRead;
    }
}