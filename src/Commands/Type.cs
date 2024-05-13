using System.Net.Sockets;
using System.Text;

namespace codecrafters_redis.Commands;

public class Type : Base
{
    public override bool CanBePropagated => false;

    protected override Task OnMasterNodeExecute(Socket socket, int commandCount, string[] commandParts,
        bool replicaConnection = false)
    {
        var key = commandParts[1];
        var cacheItem = DataCache.Get(key);
        
        socket.Send(cacheItem?.Value != null
            ? Encoding.UTF8.GetBytes("+string\r\n")
            : Encoding.UTF8.GetBytes("+none\r\n"));

        return Task.CompletedTask;
    }

    protected override Task OnReplicaNodeExecute(Socket socket, int commandCount, string[] commandParts,
        bool replicaConnection = false)
    {
        return Task.CompletedTask;
    }
}