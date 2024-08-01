using System.Net.Sockets;
using codecrafters_redis.Common;
using codecrafters_redis.Receivers;

namespace codecrafters_redis.Commands;

public class CommandDetails
{
    public int CommandCount { get; set; }
    public string[] CommandParts { get; set; }
    public string CommandString { get; set; }
    public CommandType CommandType { get; set; }
    public bool FromTransaction { get; set; }
}

public abstract class Base
{
    public abstract bool CanBePropagated { get; }
    protected bool TransactionStarted { get; set; }

    protected abstract Task<string> OnMasterNodeExecute(Socket socket, CommandDetails commandDetails,
        List<CommandQueueItem> commandQueue, ReceiverBase receiver, bool replicaConnection = false);

    protected abstract Task<string> OnReplicaNodeExecute(Socket socket, CommandDetails commandDetails,
        List<CommandQueueItem> commandQueue, ReceiverBase receiver, bool replicaConnection = false);

    public async Task<string> Execute(Socket socket, CommandDetails commandDetails, List<CommandQueueItem> commandQueue,
        ReceiverBase receiver, bool replicaConnection = false)
    {
        string result;
        
        if (TransactionEnabled(socket, commandDetails.CommandParts, commandQueue))
        {
            return string.Empty;
        }

        if (ServerInfo.ServerRuntimeContext.IsMaster)
        {
            result = await OnMasterNodeExecute(socket, commandDetails, commandQueue, receiver, replicaConnection);
        }
        else
        {
            result = await OnReplicaNodeExecute(socket, commandDetails, commandQueue, receiver, replicaConnection);
        }

        return result;
    }

    private bool TransactionEnabled(Socket socket, string[] commandParts, List<CommandQueueItem> commandQueue)
    {
        if (!TransactionStarted)
        {
            TransactionStarted = commandQueue.Count != 0;
        }

        if (!TransactionStarted)
        {
            return false;
        }

        if (string.Equals(commandParts[2], CommandType.Multi.ToString(),
                StringComparison.InvariantCultureIgnoreCase) ||
            string.Equals(commandParts[2], CommandType.Exec.ToString(),
                StringComparison.InvariantCultureIgnoreCase))
        {
            return false;
        }

        var commandString = string.Join("\r\n", commandParts);
        var commandType = commandParts[2].ToCommandType();

        commandQueue.Add(new CommandQueueItem
        {
            CommandType = commandType,
            CommandString = commandString
        });

        socket.Send("+QUEUED\r\n"u8.ToArray());
        return true;
    }
}