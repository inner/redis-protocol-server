using System.Net.Sockets;

namespace codecrafters_redis.Commands;

public abstract class Base
{
    public abstract bool CanBePropagated { get; }

    protected abstract Task OnMasterNodeExecute(Socket socket, int commandCount, string[] commandParts,
        bool replicaConnection = false);

    protected abstract Task OnReplicaNodeExecute(Socket socket, int commandCount, string[] commandParts,
        bool replicaConnection = false);

    public async Task Execute(Socket socket, int commandCount, string[] commandParts, bool replicaConnection = false)
    {
        if (ServerInfo.ServerRuntimeContext.IsMaster)
        {
            await OnMasterNodeExecute(socket, commandCount, commandParts, replicaConnection);
        }
        else
        {
            await OnReplicaNodeExecute(socket, commandCount, commandParts, replicaConnection);
        }
    }
}