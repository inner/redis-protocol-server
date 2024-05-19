using System.Net.Sockets;

namespace codecrafters_redis.Commands;

public class Xread : Base
{
    public override bool CanBePropagated => false;

    protected override Task OnMasterNodeExecute(Socket socket, int commandCount, string[] commandParts,
        bool replicaConnection = false)
    {
        throw new NotImplementedException();
    }

    protected override Task OnReplicaNodeExecute(Socket socket, int commandCount, string[] commandParts,
        bool replicaConnection = false)
    {
        throw new NotImplementedException();
    }
}