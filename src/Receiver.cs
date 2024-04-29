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
                ExecuteBulkString();
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

        if (ServerInfo.IsMaster)
        {
            foreach (var replicaSocket in ServerInfo.ReplicaSockets.Where(x => x.Value.Connected))
            {
                Console.WriteLine($"Propagating command '{commandString[..^1]}' to replica '{replicaSocket.Key}'.");
                replicaSocket.Value.Send(Encoding.UTF8.GetBytes(commandString.Replace("\\r\\n", "\r\n")));

                if (string.Equals(commandParts[4], "getack", StringComparison.InvariantCultureIgnoreCase) &&
                    string.Equals(commandParts[6], "*", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (replicaSocket.Value.Poll(1000000, SelectMode.SelectRead))
                    {
                        var buffer = new byte[1024];
                        var bytesReceived = replicaSocket.Value.Receive(buffer);
                        var response = Encoding.UTF8.GetString(buffer, 0, bytesReceived);
                        if (!response.Contains("ACK", StringComparison.InvariantCultureIgnoreCase) )
                        {
                            // Handle the case where a GETACK doesn’t receive an ACK back
                            // This could be logging the error, retrying the command, etc.
                            // this socket
                            replicaSocket.Value.Send(Encoding.UTF8.GetBytes(Constants.NullResponse));
                            Console.WriteLine("GETACK didn't receive an ACK back.");
                        }
                    }
                }
                
                command.Execute(replicaSocket.Value, commandCount, commandParts, replicaConnection: true);
            }
        }

        command.Execute(socket, commandCount, commandParts);
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

    private void ExecuteBulkString()
    {
    }
}