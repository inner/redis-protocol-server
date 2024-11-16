using codecrafters_redis.Commands.Common;
using codecrafters_redis.Common;

namespace codecrafters_redis.Commands;

public class Replconf : Base
{
    public override bool CanBePropagated => false;

    protected override Task<string> OnMasterNodeExecute(CommandContext commandContext)
    {
        var result = RespBuilder.SimpleString("OK");

        if (string.Equals(commandContext.CommandDetails.CommandParts[4], "listening-port",
                StringComparison.InvariantCultureIgnoreCase) ||
            string.Equals(commandContext.CommandDetails.CommandParts[4], "capa",
                StringComparison.InvariantCultureIgnoreCase))
        {
            commandContext.Socket.Send(result.AsBytes());
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
        var resp = RespBuilder
            .ArrayFromCommands(
                "REPLCONF",
                "ACK",
                ServerInfo.Replication.ReplicaBytesReceived.ToString());

        if (string.Equals(commandContext.CommandDetails.CommandParts[4], "GETACK",
                StringComparison.InvariantCultureIgnoreCase) &&
            string.Equals(commandContext.CommandDetails.CommandParts[6], "*",
                StringComparison.InvariantCultureIgnoreCase))
        {
            commandContext.Socket.Send(resp.AsBytes());
        }

        return Task.FromResult(resp);
    }
}