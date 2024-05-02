using System.Net.Sockets;

namespace codecrafters_redis.Commands;

public abstract class Base
{
    public abstract bool CanBePropagated { get; }
    // protected abstract void OnMasterExecute(Socket socket, int commandCount, string[] commandParts, int bytesReceived);
    // protected abstract void OnReplicaExecute(Socket socket, int commandCount, string[] commandParts, int bytesReceived);

    public abstract void Execute(Socket socket, int commandCount, string[] commandParts, int bytesReceived,
        bool replicaConnection = false);
}