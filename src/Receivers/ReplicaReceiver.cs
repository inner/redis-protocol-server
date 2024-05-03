using System.Net.Sockets;
using System.Text;

namespace codecrafters_redis.Receivers;

public class ReplicaReceiver : ReceiverBase
{
    public override void Receive(Socket socket, string commandString)
    {
        if (!ServerInfo.ReplicaHandshakeCompleted)
        {
            Console.WriteLine("Replica handshake not completed yet. Continuing...");
            // return;
        }

        base.Receive(socket, commandString);

        if (commandString.Contains("$3\r\nACK\r\n"))
        {
            return;
        }

        if (commandString.EndsWith("\r\n\n"))
        {
            commandString = commandString[..^1];
        }
        
        var currentBytesReceived = Encoding.UTF8.GetBytes(commandString).Length;
        Console.WriteLine($"Total bytes received: {ServerInfo.BytesReceived}. Incrementing bytes received by {currentBytesReceived}");
        ServerInfo.IncrementBytesReceived(currentBytesReceived);
    }
}