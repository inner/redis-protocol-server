using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using codecrafters_redis.Receivers;

namespace codecrafters_redis.Commands;

public class Wait : Base
{
    public override bool CanBePropagated => false;

    protected override async Task OnMasterNodeExecute(Socket socket, int commandCount, string[] commandParts,
        List<CommandQueueItem> commandQueue, ReceiverBase receiver, bool replicaConnection = false)
    {
        ServerInfo.Replication.ReplicaAcksReceived = 0;
        
        var numberOfReplicasToWaitFor = commandParts[4];
        var msToWait = commandParts[6];
        
        var tasks = new List<Task>();
        foreach (var replica in ServerInfo.ServerRuntimeContext.Replicas.Where(x => x.Value.Connected))
        {
            tasks.Add(Task.Run(() =>
            {
                replica.Value.Send(Encoding.UTF8.GetBytes("*3\r\n$8\r\nREPLCONF\r\n$6\r\nGETACK\r\n$1\r\n*\r\n"));
            }));
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
        
        socket.Send(Encoding.UTF8.GetBytes($":{acksReceived}\r\n"));
    }

    protected override Task OnReplicaNodeExecute(Socket socket, int commandCount, string[] commandParts,
        List<CommandQueueItem> commandQueue, ReceiverBase receiver, bool replicaConnection = false)
    {
        return Task.CompletedTask;
    }
}