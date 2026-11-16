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

        await base.Receive(socket, resp, commandQueue, subscriptions, source);

        // The replication offset counts every byte of the master's stream,
        // and only that stream: commands from ordinary clients connected to
        // this replica do not move it. It is advanced after the command ran,
        // so REPLCONF GETACK reports the offset before its own bytes.
        if (source == CommandSource.ReplicationMaster)
        {
            ServerInfo.Replication.IncrementReplicaBytesReceived(resp.AsBytes().Length);
        }
    }
}
