using System.Net.Sockets;
using System.Text;
using codecrafters_redis.Common;
using codecrafters_redis.Receivers;

namespace codecrafters_redis.Commands;

public class Replconf : Base
{
    public override bool CanBePropagated => false;

    protected override Task<string> OnMasterNodeExecute(Socket socket, CommandDetails commandDetails,
        List<CommandQueueItem> commandQueue, ReceiverBase receiver, bool replicaConnection = false)
    {
        var result = Constants.OkResponse;
        
        if (string.Equals(commandDetails.CommandParts[4], "listening-port", StringComparison.InvariantCultureIgnoreCase) ||
            string.Equals(commandDetails.CommandParts[4], "capa", StringComparison.InvariantCultureIgnoreCase))
        {
            socket.Send(Encoding.UTF8.GetBytes(result));
        }
        
        if (string.Equals(commandDetails.CommandParts[4], "ack", StringComparison.InvariantCultureIgnoreCase))
        {
            ServerInfo.Replication.IncrementReplicaAcksReceived();
            
            Console.WriteLine($"Received ACK from replica '{socket.RemoteEndPoint}', " +
                              $"bytes received: {commandDetails.CommandParts[6]}.");
            
            Console.WriteLine($"Replica ACKs received: {ServerInfo.Replication.ReplicaAcksReceived}.");
        }
        
        return Task.FromResult(result);
    }

    protected override Task<string> OnReplicaNodeExecute(Socket socket, CommandDetails commandDetails,
        List<CommandQueueItem> commandQueue, ReceiverBase receiver, bool replicaConnection = false)
    {
        if (!ServerInfo.Replication.ReplicaHandshakeCompleted)
        {
            return Task.FromResult(string.Empty);
        }

        var result =
            $"*3\r\n$8\r\nREPLCONF\r\n$3\r\nACK\r\n${ServerInfo.Replication.ReplicaBytesReceived.ToString().Length}\r\n{ServerInfo.Replication.ReplicaBytesReceived}\r\n";
        
        if (string.Equals(commandDetails.CommandParts[4], "getack",
                StringComparison.InvariantCultureIgnoreCase) &&
            string.Equals(commandDetails.CommandParts[6], "*", StringComparison.InvariantCultureIgnoreCase))
        {
            socket.Send(Encoding.UTF8.GetBytes(result));
        }
        
        return Task.FromResult(result);
    }
}