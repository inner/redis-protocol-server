using System.Net.Sockets;
using System.Text;
using codecrafters_redis.Commands;

namespace codecrafters_redis.Receivers;

public class ReplicaReceiver : ReceiverBase
{
    public override async Task Receive(Socket socket, string commandString, List<CommandQueueItem> commandQueue)
    {
        if (!ServerInfo.Replication.ReplicaHandshakeCompleted)
        {
            return;
        }
        
        if (!ServerInfo.Replication.ReplicaFirstByteReceived || commandString.Contains("$3\r\nACK\r\n"))
        {
            ServerInfo.Replication.ReplicaFirstByteReceived = true;
            await base.Receive(socket, commandString, commandQueue);
            return;
        }

        if (commandString.EndsWith("\r\n\n"))
        {
            commandString = commandString[..^1];
        }

        var currentBytesReceived = Encoding.UTF8.GetBytes(commandString).Length;
        
        Console.WriteLine(
            $"Total bytes received: {ServerInfo.Replication.ReplicaBytesReceived}. " +
            $"Incrementing bytes received by {currentBytesReceived}");
        
        ServerInfo.Replication.IncrementReplicaBytesReceived(currentBytesReceived);

        await base.Receive(socket, commandString, commandQueue);
    }
}