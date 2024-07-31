using System.Collections.Concurrent;
using System.Net.Sockets;
using codecrafters_redis.Enums;

namespace codecrafters_redis.Commands;

public abstract class Base
{
    public abstract bool CanBePropagated { get; }
    protected bool TransactionStarted { get; private set; }

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

        if (TransactionStarted && (!string.Equals(commandParts[2], CommandTypes.Multi.ToString(),
                StringComparison.InvariantCultureIgnoreCase) && !string.Equals(commandParts[2],
                CommandTypes.Exec.ToString(),
                StringComparison.InvariantCultureIgnoreCase)))
        {
            concurrentQueue.Enqueue(commandParts[2]);
            socket.Send("+QUEUED\r\n"u8.ToArray());
            return;
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