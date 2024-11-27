using System.Diagnostics;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Wait : Base
{
    protected override string Name => nameof(Wait);
    public override bool CanBePropagated => false;

    protected override async Task<string> OnMasterNodeExecute(CommandContext commandContext)
    {
        ServerInfo.Replication.ReplicaAcksReceived = 0;

        var numberOfReplicasToWaitFor = commandContext.CommandDetails.CommandParts[4];
        var msToWait = commandContext.CommandDetails.CommandParts[6];

        var tasks = new List<Task>();
        var getAckResp = RespBuilder.ArrayFromCommands("REPLCONF", "GETACK", "*");

        var connectedReplicas = ServerInfo.ServerRuntimeContext.Replicas
            .Where(x => x.Value.Connected);

        foreach (var replica in connectedReplicas)
        {
            tasks.Add(Task.Run(() =>
                replica.Value.SendCommand(getAckResp)));
        }

        await Task.WhenAll(tasks);

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