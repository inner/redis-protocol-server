using System.Net.Sockets;
using System.Text;

namespace codecrafters_redis.Receivers;

public class ReplicaReceiver : ReceiverBase
{
    public override void Receive(Socket socket, string commandString)
    {
        base.Receive(socket, commandString);
        
        if (!ServerInfo.ReplicaHandshakeCompleted)
        {
            return;
        }
        
        var currentBytesRead = Encoding.UTF8.GetBytes(commandString).Length;
        ServerInfo.BytesReceived += currentBytesRead;
    }
}