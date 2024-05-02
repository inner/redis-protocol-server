using System.Net.Sockets;

namespace codecrafters_redis.Commands;

public abstract class Base
{
    public abstract bool CanBePropagated { get; }

    protected abstract void OnMasterNodeExecute(Socket socket, int commandCount, string[] commandParts,
        bool replicaConnection = false);

    protected abstract void OnReplicaNodeExecute(Socket socket, int commandCount, string[] commandParts,
        bool replicaConnection = false);

    public void Execute(Socket socket, int commandCount, string[] commandParts, bool replicaConnection = false)
    {
        if (ServerInfo.IsMaster)
        {
            OnMasterNodeExecute(socket, commandCount, commandParts, replicaConnection);
        }
        else
        {
            OnReplicaNodeExecute(socket, commandCount, commandParts, replicaConnection);
        }
    }
}