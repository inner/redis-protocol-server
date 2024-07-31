using System.Collections.Concurrent;
using System.Net.Sockets;

namespace codecrafters_redis.Commands;

public abstract class Base
{
    public abstract bool CanBePropagated { get; }
    public bool TransactionStarted { get; private set; }

    protected abstract Task OnMasterNodeExecute(Socket socket, int commandCount, string[] commandParts,
        ConcurrentQueue<string> concurrentQueue, bool replicaConnection = false);

    protected abstract Task OnReplicaNodeExecute(Socket socket, int commandCount, string[] commandParts,
        ConcurrentQueue<string> concurrentQueue, bool replicaConnection = false);

    public async Task Execute(Socket socket, int commandCount, string[] commandParts,
        ConcurrentQueue<string> concurrentQueue, bool replicaConnection = false)
    {
        if (!TransactionStarted)
        {
            TransactionStarted = !concurrentQueue.IsEmpty;
        }

        if (ServerInfo.ServerRuntimeContext.IsMaster)
        {
            await OnMasterNodeExecute(socket, commandCount, commandParts, concurrentQueue, replicaConnection);
        }
        else
        {
            await OnReplicaNodeExecute(socket, commandCount, commandParts, concurrentQueue, replicaConnection);
        }
    }
}