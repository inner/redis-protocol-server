using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using codecrafters_redis.Commands;
using codecrafters_redis.Common;

namespace codecrafters_redis.Receivers;

public abstract class ReceiverBase
{
    public virtual async Task Receive(Socket socket, string commandString, List<CommandQueueItem> commandQueue,
        bool countBytes = true)
    {
        try
        {
            commandString = commandString.Replace("\r\n", "\\r\\n");
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
                    ExecuteBulkString(socket, commandString);
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
            var commandDetails = BuildCommandDetails(commandString, commandToExecute);
            await ExecuteCommand(socket, commandDetails, commandQueue);
        }
    }

    private static CommandDetails BuildCommandDetails(string commandString, string commandToExecute)
    {
        var commandParts = commandString.Split("\\r\\n")
            .Where(x => !string.IsNullOrEmpty(x))
            .ToArray();

        var commandDetails = new CommandDetails
        {
            CommandString = commandToExecute,
            CommandParts = commandParts,
            CommandCount = int.Parse(commandParts[0].Replace("*", string.Empty)),
            CommandType = commandParts[2].ToCommandType()
        };

        return commandDetails;
    }

    public async Task ExecuteCommand(Socket socket, CommandDetails commandDetails, List<CommandQueueItem> commandQueue)
    {
        var className = $"codecrafters_redis.Commands.{commandDetails.CommandType}";
        var type = System.Type.GetType(className);

        if (type == null)
        {
            throw new ArgumentException("Unknown RESP command.");
        }

        var command = (Base)Activator.CreateInstance(type)!;
        await command.Execute(socket, commandDetails, commandQueue, this);

        if (!ServerInfo.ServerRuntimeContext.IsMaster || !command.CanBePropagated ||
            commandDetails.CommandString.Contains("$3\r\nACK\r\n"))
        {
            return;
        }

        SendToReplicas(commandDetails.CommandString);
    }

    private static void SendToReplicas(string commandString)
    {
        foreach (var replica in ServerInfo.ServerRuntimeContext.Replicas.Where(x => x.Value.Connected))
        {
            Console.WriteLine(
                $"Propagating command '{commandString[..^1]}' to replica '{replica.Value.RemoteEndPoint}'.");

            replica.Value.Send(Encoding.UTF8.GetBytes(commandString.Replace("\\r\\n", "\r\n")));
        }
    }

    private void ExecuteBulkString(Socket socket, string commandString)
    {
        var commandParts = commandString.Split("\\r\\n");
        socket.Send("*3\r\n$8\r\nREPLCONF\r\n$3\r\nACK\r\n$1\r\n0\r\n"u8.ToArray());
    }

    private void ExecuteSimpleString()
    {
    }

    private void ExecuteSimpleError()
    {
    }

    private void ExecuteInteger()
    {
    }
}