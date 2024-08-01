using System.Net.Sockets;
using codecrafters_redis.Enums;
using codecrafters_redis.Receivers;

namespace codecrafters_redis.Commands;

public abstract class Base
{
    public abstract bool CanBePropagated { get; }
    private bool TransactionStarted { get; set; }

    protected abstract Task OnMasterNodeExecute(Socket socket, int commandCount,
        string[] commandParts, List<CommandQueueItem> commandQueue, ReceiverBase receiver,
        bool replicaConnection = false);

    protected abstract Task OnReplicaNodeExecute(Socket socket, int commandCount,
        string[] commandParts, List<CommandQueueItem> commandQueue, ReceiverBase receiver,
        bool replicaConnection = false);

    public async Task Execute(Socket socket, int commandCount, string[] commandParts,
        List<CommandQueueItem> commandQueue, ReceiverBase receiver, bool replicaConnection = false)
    {
        if (!TransactionStarted)
        {
            TransactionStarted = commandQueue.Count != 0;
        }

        if (TransactionStarted &&
            !string.Equals(commandParts[2], CommandType.Multi.ToString(),
                StringComparison.InvariantCultureIgnoreCase) &&
            !string.Equals(commandParts[2], CommandType.Exec.ToString(), StringComparison.InvariantCultureIgnoreCase))
        {
            var commandString = string.Join("\r\n", commandParts);
            var commandType = commandParts[2].ToCommandType();
            
            commandQueue.Add(new CommandQueueItem
            {
                CommandType = commandType,
                CommandString = commandString
            });
            
            socket.Send("+QUEUED\r\n"u8.ToArray());
            return;
        }

        if (ServerInfo.ServerRuntimeContext.IsMaster)
        {
            await OnMasterNodeExecute(socket, commandCount, commandParts, commandQueue, receiver,
                replicaConnection);
        }
        else
        {
            await OnReplicaNodeExecute(socket, commandCount, commandParts, commandQueue, receiver,
                replicaConnection);
        }
    }
}