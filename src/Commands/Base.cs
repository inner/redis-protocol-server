using codecrafters_redis.Common;

namespace codecrafters_redis.Commands;

public abstract class Base
{
    public abstract bool CanBePropagated { get; }
    private bool TransactionStarted { get; set; }

    protected virtual Task<string> OnMasterNodeExecute(CommandContext commandContext)
    {
        return Task.FromResult(string.Empty);
    }

    protected virtual Task<string> OnReplicaNodeExecute(CommandContext commandContext)
    {
        return Task.FromResult(string.Empty);
    }

    public async Task<string> Execute(CommandContext commandContext)
    {
        if (TransactionEnabled(commandContext))
        {
            return string.Empty;
        }

        var result = ServerInfo.ServerRuntimeContext.IsMaster switch
        {
            true => await OnMasterNodeExecute(commandContext),
            false => await OnReplicaNodeExecute(commandContext)
        };

        return result;
    }

    private bool TransactionEnabled(CommandContext commandContext)
    {
        if (!TransactionStarted)
        {
            TransactionStarted = commandContext.CommandQueue.Count != 0;
        }

        if (!TransactionStarted)
        {
            return false;
        }

        var commandTypesExcluded = new List<CommandType>
        {
            CommandType.Multi, CommandType.Exec, CommandType.Discard
        };

        var commandStrings = commandTypesExcluded.ConvertAll(c => c.ToString());
        
        if (commandStrings.Exists(c =>
                string.Equals(commandContext.CommandDetails.CommandParts[2], c,
                    StringComparison.InvariantCultureIgnoreCase)))
        {
            return false;
        }

        var commandString = string.Join("\r\n", commandContext.CommandDetails.CommandParts);
        var commandType = commandContext.CommandDetails.CommandParts[2].ToCommandType();

        commandContext.CommandQueue.Add(new CommandQueueItem
        {
            CommandType = commandType,
            CommandString = commandString
        });

        commandContext.Socket.Send("+QUEUED\r\n"u8.ToArray());
        return true;
    }
}