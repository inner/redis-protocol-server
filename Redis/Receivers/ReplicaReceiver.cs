using System.Net.Sockets;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Receivers;

public class ReplicaReceiver : ReceiverBase
{
    public override async Task Receive(Socket socket, string resp, List<CommandQueueItem> commandQueue,
        List<string> subscriptions, CommandSource source)
    {
        if (!ServerInfo.Replication.ReplicaHandshakeCompleted)
            return;

        if (!ServerInfo.Replication.ReplicaFirstByteReceived)
        {
            ServerInfo.Replication.ReplicaFirstByteReceived = true;
            await base.Receive(socket, resp, commandQueue, subscriptions, source);
            return;
        }

        var currentBytesReceived = GetCurrentBytesReceived(resp);

        Console.WriteLine(
            $"Total bytes received: {ServerInfo.Replication.ReplicaBytesReceived}. " +
            $"Incrementing bytes received by {currentBytesReceived}");

        ServerInfo.Replication.IncrementReplicaBytesReceived(currentBytesReceived);
        await base.Receive(socket, resp, commandQueue, subscriptions, source);
    }

    private static int GetCurrentBytesReceived(string resp)
    {
        var currentBytesReceived = resp
            .Replace(Constants.VerbatimNewLine, Constants.NewLine)
            .AsBytes()
            .Length;

        return currentBytesReceived;
    }
}
