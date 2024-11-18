using System.Net.Sockets;
using codecrafters_redis.Commands.Common;
using codecrafters_redis.Common;

namespace codecrafters_redis.Receivers;

public class ReplicaReceiver : ReceiverBase
{
    public override async Task Receive(Socket socket, string commandString, List<CommandQueueItem> commandQueue)
    {
        if (!ServerInfo.Replication.ReplicaHandshakeCompleted) return;
        
        if (!ServerInfo.Replication.ReplicaFirstByteReceived)
        {
            ServerInfo.Replication.ReplicaFirstByteReceived = true;
            await base.Receive(socket, commandString, commandQueue);
            return;
        }

        var currentBytesReceived = commandString.AsBytes().Length;
        
        Console.WriteLine(
            $"Total bytes received: {ServerInfo.Replication.ReplicaBytesReceived}. " +
            $"Incrementing bytes received by {currentBytesReceived}");
        
        ServerInfo.Replication.IncrementReplicaBytesReceived(currentBytesReceived);

        await base.Receive(socket, commandString, commandQueue);
    }
}