using System.Diagnostics;
using System.Text;

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
        foreach (var replica in ServerInfo.ServerRuntimeContext.Replicas.Where(x => x.Value.Connected))
        {
            tasks.Add(Task.Run(() =>
            {
                replica.Value.Send("*3\r\n$8\r\nREPLCONF\r\n$6\r\nGETACK\r\n$1\r\n*\r\n"u8.ToArray());
            }));
        }

        await Task.WhenAll(tasks);

        var startingTimestamp = Stopwatch.GetTimestamp();
        while (Stopwatch.GetElapsedTime(startingTimestamp).Milliseconds < int.Parse(msToWait))
        {
            if (ServerInfo.Replication.ReplicaAcksReceived >= int.Parse(numberOfReplicasToWaitFor))
            {
                break;
            }
        }

        var acksReceived = ServerInfo.Replication.ReplicaAcksReceived == 0
            ? ServerInfo.ServerRuntimeContext.GetConnectedReplicas()
            : ServerInfo.Replication.ReplicaAcksReceived;
        
        var result = $":{acksReceived}\r\n";
        commandContext.Socket.Send(Encoding.UTF8.GetBytes(result));
        return result;
    }
}