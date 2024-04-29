using System.Net.Sockets;

namespace codecrafters_redis.Commands;

public abstract class Base
{
    public abstract bool IsPropagated { get; }

    public abstract void Execute(Socket socket, int commandCount, string[] commandParts,
        bool replicaConnection = false);
}