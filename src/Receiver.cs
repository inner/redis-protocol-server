using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using codecrafters_redis.Commands;
using codecrafters_redis.Enums;

namespace codecrafters_redis;

public class Receiver
{
    public void Receive(Socket socket, string commandString)
    {
        commandString = commandString.Replace("\r\n", "\\r\\n");
        var respDataType = commandString.GetRespDataType();

        switch (respDataType)
        {
            case DataType.Array:
                ExecuteAsArrayMultiCommand(socket, commandString);
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

    private void ExecuteAsArrayMultiCommand(Socket socket, string commandString)
    {
        var multiCommandSplit = Regex.Split(commandString, @"(\*\d+\\r\\n)")
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
        
        List<string> commandsToExecute = [];
                
        var skip = 0;
        for (var i = 0; i < multiCommandSplit.Count/2; i++)
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
            ExecuteArray(socket, commandToExecute);
        }
    }

    private void ExecuteArray(Socket socket, string commandString)
    {
        var commandParts = commandString.Split("\\r\\n");
        var commandCount = int.Parse(commandParts[0].Replace("*", string.Empty));
        var commandType = commandParts[2].ToCommandType();

        ExecuteCommand(socket, commandString, commandType, commandCount, commandParts);
    }

    private void ExecuteCommand(Socket socket, string commandString, CommandType commandType, int commandCount,
        string[] commandParts)
    {
        var className = $"codecrafters_redis.Commands.{commandType}";
        var type = Type.GetType(className);

        if (type == null)
        {
            throw new ArgumentException("Unknown RESP command.");
        }

        var command = (Base)Activator.CreateInstance(type)!;

        if (ServerInfo.IsMaster && new[] { CommandType.Set }.Contains(commandType))
        {
            foreach (var replicaSocket in ServerInfo.ReplicaSockets.Where(x => x.Value.Connected))
            {
                Console.WriteLine($"Propagating command '{commandString}' to replica '{replicaSocket.Key}'.");
                // command.Execute(replicaSocket.Value, commandCount, commandParts, replicaConnection: true);
                
                replicaSocket.Value.Send(Encoding.UTF8.GetBytes(commandString.Replace("\\r\\n", "\r\n")));
            }
        }

        command.Execute(socket, commandCount, commandParts);
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