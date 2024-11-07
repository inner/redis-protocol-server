using System.Diagnostics;
using System.Text;
using codecrafters_redis.Common;

namespace codecrafters_redis.Commands;

public class Wait : Base
{
    public override bool CanBePropagated => false;

    protected override async Task<string> OnMasterNodeExecute(CommandContext commandContext)
    {
        ServerInfo.Replication.ReplicaAcksReceived = 0;

        var numberOfReplicasToWaitFor = commandContext.CommandDetails.CommandParts[4];
        var msToWait = commandContext.CommandDetails.CommandParts[6];

        var tasks = new List<Task>();
        var resp = RespBuilder.BuildRespArray("REPLCONF", "GETACK", "*");
        
        var connectedReplicas = ServerInfo.ServerRuntimeContext.Replicas
            .Where(x => x.Value.Connected);
        
        foreach (var replica in connectedReplicas)
        {
            tasks.Add(Task.Run(() =>
                replica.Value.Send(resp.AsBytes())));
        }

        await Task.WhenAll(tasks);

        var sw = new Stopwatch();
        sw.Start();

        while (sw.ElapsedMilliseconds < int.Parse(msToWait))
        {
            if (ServerInfo.Replication.ReplicaAcksReceived >= int.Parse(numberOfReplicasToWaitFor))
            {
                break;
            }
        }

        sw.Stop();

        var acksReceived = ServerInfo.Replication.ReplicaAcksReceived == 0
            ? ServerInfo.ServerRuntimeContext.GetConnectedReplicas()
            : ServerInfo.Replication.ReplicaAcksReceived;

        var result = $":{acksReceived}\r\n";
        commandContext.Socket.Send(Encoding.UTF8.GetBytes(result));
        return result;
    }
}