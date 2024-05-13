using System.Diagnostics;
using System.Net.Sockets;
using System.Text;

namespace codecrafters_redis.Commands;

public class Wait : Base
{
    public override bool CanBePropagated => false;

    protected override async Task OnMasterNodeExecute(Socket socket, int commandCount, string[] commandParts,
        bool replicaConnection = false)
    {
        ServerInfo.ReplicaAcksReceived = 0;
        
        var numberOfReplicasToWaitFor = commandParts[4];
        var msToWait = commandParts[6];
        
        var tasks = new List<Task>();
        foreach (var replica in ServerInfo.Replicas.Where(x => x.Value.Connected))
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
            if (ServerInfo.ReplicaAcksReceived >= int.Parse(numberOfReplicasToWaitFor))
            {
                break;
            }
        }
        
        sw.Stop();

        var acksReceived = ServerInfo.ReplicaAcksReceived == 0
            ? ServerInfo.GetConnectedReplicas()
            : ServerInfo.ReplicaAcksReceived;
        
        socket.Send(Encoding.UTF8.GetBytes($":{acksReceived}\r\n"));
    }

    protected override Task OnReplicaNodeExecute(Socket socket, int commandCount, string[] commandParts,
        bool replicaConnection = false)
    {
        return Task.CompletedTask;
    }
}