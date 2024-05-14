using System.Net.Sockets;
using System.Text;

namespace codecrafters_redis.Commands;

public class Xadd : Base
{
    public override bool CanBePropagated => true;

    protected override Task OnMasterNodeExecute(Socket socket, int commandCount, string[] commandParts,
        bool replicaConnection = false)
    {
        var key = commandParts[4];
        
        var value = new Dictionary<string, string>
        {
            { "Id", commandParts[6] }
        };

        for (var i = 8; i < commandParts.Length; i += 2)
        {
            var valueIndex = i + 2;
            value[commandParts[i]] = commandParts[valueIndex];
            i += 2;
            if (valueIndex + 2 >= commandParts.Length - 1)
            {
                break;
            }
        }
        
        var entryId = DataCache.Xadd(key, value);
        socket.Send(Encoding.UTF8.GetBytes($"+{entryId}\r\n"));
        
        return Task.CompletedTask;
    }

    protected override Task OnReplicaNodeExecute(Socket socket, int commandCount, string[] commandParts,
        bool replicaConnection = false)
    {
        throw new NotImplementedException();
    }
}