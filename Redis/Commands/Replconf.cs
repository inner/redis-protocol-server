using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Replconf : Base
{
    public override bool CanBePropagated => false;

    public override Dictionary<string, Dictionary<string, string>> Docs()
    {
        return new()
        {
            {
                "REPLCONF",
                new()
                {
                    {"summary", "An internal command for configuring the replication stream."}
                }
            }
        };
    }

    protected override Task<string> OnMasterNodeExecute(CommandContext commandContext)
    {
        var result = RespBuilder.SimpleString("OK");

        if (string.Equals(commandContext.CommandDetails.CommandParts[4], "listening-port",
                StringComparison.InvariantCultureIgnoreCase) ||
            string.Equals(commandContext.CommandDetails.CommandParts[4], "capa",
                StringComparison.InvariantCultureIgnoreCase))
        {
            commandContext.Socket.SendCommand(result);
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
            commandContext.Socket.SendCommand(resp);
        }

        return Task.FromResult(resp);
    }
}