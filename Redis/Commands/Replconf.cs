using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Replconf : Base
{
    protected override string Name => nameof(Replconf);
    public override bool CanBePropagated => false;

    protected override Task<string> OnMasterNodeExecute(CommandContext commandContext)
    {
        var result = RespBuilder.SimpleString("OK");
        var replconfArg = commandContext.CommandDetails.CommandParts[4];

        string[] handshakeArgs = ["listening-port", "capa"];
        string[] ackArgs = ["ack"];

        if (handshakeArgs.Contains(replconfArg, StringComparer.InvariantCultureIgnoreCase))
        {
            commandContext.Socket.SendCommand(result);
        }

        if (ackArgs.Contains(replconfArg, StringComparer.InvariantCultureIgnoreCase))
        {
            ServerInfo.Replication.IncrementReplicaAcksReceived();
        }

        return Task.FromResult(result);
    }

    protected override Task<string> OnReplicaNodeExecute(CommandContext commandContext)
    {
        var replconfArg1 = commandContext.CommandDetails.CommandParts[4];
        var replconfArg2 = commandContext.CommandDetails.CommandParts[6];

        var resp = RespBuilder
            .ArrayFromCommands(
                "REPLCONF",
                "ACK",
                ServerInfo.Replication.ReplicaBytesReceived.ToString());

        if (string.Equals(replconfArg1, "GETACK", StringComparison.InvariantCultureIgnoreCase) &&
            string.Equals(replconfArg2, "*", StringComparison.InvariantCultureIgnoreCase))
        {
            commandContext.Socket.SendCommand(resp);
        }

        return Task.FromResult(resp);
    }
}
