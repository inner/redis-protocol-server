using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using codecrafters_redis.Commands;
using codecrafters_redis.Enums;

namespace codecrafters_redis.Receivers;

public abstract class ReceiverBase
{
    public virtual async Task Receive(Socket socket, string commandString)
    {
        try
        {
            commandString = commandString.Replace("\r\n", "\\r\\n");
            var respDataType = commandString.GetRespDataType();

            switch (respDataType)
            {
                case DataTypes.Array:
                    await ExecuteAsArrayMultiCommand(socket, commandString);
                    break;
                case DataTypes.SimpleString:
                    ExecuteSimpleString();
                    break;
                case DataTypes.SimpleError:
                    ExecuteSimpleError();
                    break;
                case DataTypes.Integer:
                    ExecuteInteger();
                    break;
                case DataTypes.BulkString:
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

    private async Task ExecuteAsArrayMultiCommand(Socket socket, string commandString)
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
            await ExecuteArray(socket, commandToExecute);
        }
    }

    private async Task ExecuteArray(Socket socket, string commandString)
    {
        var commandParts = commandString.Split("\\r\\n")
            .Where(x => !string.IsNullOrEmpty(x))
            .ToArray();
        
        var commandCount = int.Parse(commandParts[0].Replace("*", string.Empty));
        var commandType = commandParts[2].ToCommandType();

        await ExecuteCommand(socket, commandString, commandType, commandCount, commandParts);
    }

    private async Task ExecuteCommand(Socket socket, string commandString, CommandTypes commandTypes, int commandCount,
        string[] commandParts)
    {
        var className = $"codecrafters_redis.Commands.{commandTypes}";
        var type = System.Type.GetType(className);

        if (type == null)
        {
            throw new ArgumentException("Unknown RESP command.");
        }

        var command = (Base)Activator.CreateInstance(type)!;
        await command.Execute(socket, commandCount, commandParts);

        if (!ServerInfo.ServerRuntimeContext.IsMaster || !command.CanBePropagated || commandString.Contains("$3\r\nACK\r\n"))
        {
            return;
        }
        
        foreach (var replica in ServerInfo.ServerRuntimeContext.Replicas.Where(x => x.Value.Connected))
        {
            Console.WriteLine($"Propagating command '{commandString[..^1]}' to replica '{replica.Value.RemoteEndPoint}'.");
            replica.Value.Send(Encoding.UTF8.GetBytes(commandString.Replace("\\r\\n", "\r\n")));
        }
    }

    private void ExecuteBulkString(Socket socket, string commandString)
    {
        var commandParts = commandString.Split("\\r\\n");
        socket.Send(Encoding.UTF8.GetBytes("*3\r\n$8\r\nREPLCONF\r\n$3\r\nACK\r\n$1\r\n0\r\n"));
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