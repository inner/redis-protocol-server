using System.Diagnostics;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Wait : Base
{
    protected override string Name => nameof(Wait);
    public override bool CanBePropagated => false;

    public override Dictionary<string, Dictionary<string, string>> Docs()
    {
        return new()
        {
            {
                Name,
                new()
                {
                    {
                        "summary",
                        "Blocks until the asynchronous replication of all preceding " +
                        "write commands sent by the connection is completed."
                    },
                    { "documentation", "https://redis.io/docs/latest/commands/wait/" },
                    { "usage #1", "redis-cli" },
                    { "usage #2", "SET mykey1 myval1" },
                    { "usage #4", "SET mykey2 myval2" },
                    { "usage #5", "SET mykey3 myval3" },
                    { "usage #6", "WAIT 4 2000" }
                }
            }
        };
    }

    protected override async Task<string> OnMasterNodeExecute(CommandContext commandContext)
    {
        ServerInfo.Replication.ReplicaAcksReceived = 0;

        var numberOfReplicasToWaitFor = commandContext.CommandDetails.CommandParts[4];
        var msToWait = commandContext.CommandDetails.CommandParts[6];

        var getAckResp = RespBuilder.ArrayFromCommands("REPLCONF", "GETACK", "*");
        await ServerRuntimeContext.ExecuteOnReplicas(getAckResp);

        var startTimestamp = Stopwatch.GetTimestamp();
        while ((int)Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds < int.Parse(msToWait))
        {
            if (ServerInfo.Replication.ReplicaAcksReceived >= int.Parse(numberOfReplicasToWaitFor))
            {
                break;
            }
        }

        var acksReceived = ServerInfo.Replication.ReplicaAcksReceived == 0
            ? ServerInfo.ServerRuntimeContext.GetConnectedReplicas()
            : ServerInfo.Replication.ReplicaAcksReceived;

        var acksReceivedResp = RespBuilder.Integer(acksReceived);
        commandContext.Socket.SendCommand(acksReceivedResp);
        return acksReceivedResp;
    }
}