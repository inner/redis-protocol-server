using System.Net.Sockets;
using System.Text.RegularExpressions;
using codecrafters_redis.Commands.Common;
using codecrafters_redis.Common;

namespace codecrafters_redis.Receivers;

public abstract class ReceiverBase
{
    public virtual async Task Receive(Socket socket, string commandString, List<CommandQueueItem> commandQueue)
    {
        try
        {
            var respDataType = commandString.GetRespDataType();

            switch (respDataType)
            {
                case DataType.Array:
                    await ExecuteAsArrayMultiCommand(socket, commandString, commandQueue);
                    break;
                case DataType.SimpleString:
                    ExecuteSimpleString();
                    break;
                case DataType.SimpleError:
                    ExecuteSimpleError();
                    break;
                case DataType.Integer:
                    ExecuteInteger();
                    break;
                case DataType.BulkString:
                    ExecuteBulkString(socket);
                    break;
                default:
                    throw new ArgumentException("Invalid data type.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Some exception occured: {ex.Message}, stack: {ex.StackTrace}");
            throw;
        }
    }

    private async Task ExecuteAsArrayMultiCommand(Socket socket, string commandString,
        List<CommandQueueItem> commandQueue)
    {
        commandString = commandString.Replace("\r\n", @"\r\n");
        var multiCommandSplit = Regex.Split(commandString, @"(\*\d+\\r\\n)")
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.TrimEnd())
            .ToList();

        List<string> commandsToExecute = [];

        var skip = 0;
        for (var i = 0; i < multiCommandSplit.Count / 2; i++)
        {
            var commandToExecute = string.Join(
                string.Empty,
                multiCommandSplit
                    .Skip(skip)
                    .Take(2));

            commandsToExecute.Add(commandToExecute);
            skip += 2;
        }

        foreach (var commandToExecute in commandsToExecute)
        {
            var commandDetails = commandToExecute.BuildCommandDetails();
            await ExecuteCommand(socket, commandDetails, commandQueue);
        }
    }

    public async Task<string> ExecuteCommand(Socket socket, CommandDetails commandDetails,
        List<CommandQueueItem> commandQueue)
    {
        var className = $"codecrafters_redis.Commands.{commandDetails.CommandType}";

        var type = Type.GetType(className);
        if (type == null)
        {
            throw new ArgumentException("Unknown RESP command.");
        }

        var command = (Base)Activator.CreateInstance(type)!;

        var commandContext = new CommandContext
        {
            Socket = socket,
            CommandDetails = commandDetails,
            CommandQueue = commandQueue,
            Receiver = this,
            ReplicaConnection = false
        };

        var result = await command.Execute(commandContext);

        if (!ShouldReplicateCommand(command, commandDetails))
        {
            return result;
        }

        ExecuteOnReplicas(commandDetails.CommandString);

        return result;
    }

    private static void ExecuteOnReplicas(string commandString)
    {
        foreach (var replica in ServerInfo.ServerRuntimeContext.Replicas.Where(x => x.Value.Connected))
        {
            Console.WriteLine($"Propagating command '{commandString[..^1]}' " +
                              $"to replica '{replica.Value.RemoteEndPoint}'.");

            replica.Value.Send(commandString.Replace(@"\r\n", "\r\n").AsBytes());
        }
    }

    private static bool ShouldReplicateCommand(Base command, CommandDetails commandDetails)
    {
        // do not replicate the command if:
        // - the server is not a master
        // - the command cannot be propagated
        // - the command is an ACK response
        
        return ServerInfo.ServerRuntimeContext.IsMaster &&
               command.CanBePropagated &&
               !commandDetails.CommandString.Contains("$3\r\nACK\r\n");
    }

    private void ExecuteBulkString(Socket socket)
    {
        var resp = RespBuilder.ArrayFromCommands("REPLCONF", "ACK", "0");
        socket.Send(resp.AsBytes());
    }

    private static void ExecuteSimpleString()
    {
    }

    private static void ExecuteSimpleError()
    {
    }

    private static void ExecuteInteger()
    {
    }
}