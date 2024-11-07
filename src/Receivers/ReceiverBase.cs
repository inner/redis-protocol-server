using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using codecrafters_redis.Commands;
using codecrafters_redis.Common;

namespace codecrafters_redis.Receivers;

public abstract class ReceiverBase
{
    public virtual async Task Receive(Socket socket, string commandString, List<CommandQueueItem> commandQueue)
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

        var type = System.Type.GetType(className);
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


        if (ServerInfo.ServerRuntimeContext.IsMaster && command.CanBePropagated &&
            !commandDetails.CommandString.Contains("$3\r\nACK\r\n"))
        {
            ExecuteOnReplicas(commandDetails.CommandString);
        }
        
        return commandDetails.FromTransaction
            ? await command.Execute(commandContext)
            : string.Empty;
    }

    private static void ExecuteOnReplicas(string commandString)
    {
        foreach (var replica in ServerInfo.ServerRuntimeContext.Replicas.Where(x => x.Value.Connected))
        {
            Console.WriteLine(
                $"Propagating command '{commandString[..^1]}' to replica '{replica.Value.RemoteEndPoint}'.");

            replica.Value.Send(Encoding.UTF8.GetBytes(commandString.Replace("\\r\\n", "\r\n")));
        }
    }

    private void ExecuteBulkString(Socket socket)
    {
        socket.Send("*3\r\n$8\r\nREPLCONF\r\n$3\r\nACK\r\n$1\r\n0\r\n"u8.ToArray());
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