using System.Net.Sockets;
using System.Text;

namespace codecrafters_redis.Receivers;

public class ReplicaReceiver : ReceiverBase
{
    public override void Receive(Socket socket, string commandString)
    {
        Console.WriteLine($"Managed thread: {Thread.CurrentThread.ManagedThreadId}");
        
        if (!ServerInfo.ReplicaHandshakeCompleted)
        {
            return;
        }
        
        base.Receive(socket, commandString);
        
        var currentBytesReceived = Encoding.UTF8.GetBytes(commandString).Length;
        ServerInfo.IncrementBytesReceived(currentBytesReceived);
    }
}