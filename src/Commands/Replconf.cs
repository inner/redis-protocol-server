using System.Text;
using codecrafters_redis.Common;

namespace codecrafters_redis.Commands;

public class Replconf : Base
{
    public override bool CanBePropagated => false;

    protected override Task<string> OnMasterNodeExecute(CommandContext commandContext)
    {
        var result = Constants.OkResponse;

        if (string.Equals(commandContext.CommandDetails.CommandParts[4], "listening-port",
                StringComparison.InvariantCultureIgnoreCase) ||
            string.Equals(commandContext.CommandDetails.CommandParts[4], "capa",
                StringComparison.InvariantCultureIgnoreCase))
        {
            commandContext.Socket.Send(Encoding.UTF8.GetBytes(result));
        }

        if (string.Equals(commandContext.CommandDetails.CommandParts[4], "ack",
                StringComparison.InvariantCultureIgnoreCase))
        {
            ServerInfo.Replication.IncrementReplicaAcksReceived();

            Console.WriteLine($"Received ACK from replica '{commandContext.Socket.RemoteEndPoint}', " +
                              $"bytes received: {commandContext.CommandDetails.CommandParts[6]}.");

            Console.WriteLine($"Replica ACKs received: {ServerInfo.Replication.ReplicaAcksReceived}.");
        }

        return Task.FromResult(result);
    }

    protected override Task<string> OnReplicaNodeExecute(CommandContext commandContext)
    {
        if (!ServerInfo.Replication.ReplicaHandshakeCompleted)
        {
            return Task.FromResult(string.Empty);
        }

        var result =
            $"*3\r\n$8\r\nREPLCONF\r\n$3\r\nACK\r\n${ServerInfo.Replication.ReplicaBytesReceived.ToString().Length}\r\n{ServerInfo.Replication.ReplicaBytesReceived}\r\n";

        if (string.Equals(commandContext.CommandDetails.CommandParts[4], "getack",
                StringComparison.InvariantCultureIgnoreCase) &&
            string.Equals(commandContext.CommandDetails.CommandParts[6], "*",
                StringComparison.InvariantCultureIgnoreCase))
        {
            commandContext.Socket.Send(Encoding.UTF8.GetBytes(result));
        }

        return Task.FromResult(result);
    }
}