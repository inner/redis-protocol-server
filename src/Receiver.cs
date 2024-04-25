using System.Net.Sockets;
using System.Text;
using codecrafters_redis.Commands;
using codecrafters_redis.Enums;
using codecrafters_redis.Network;

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
                ExecuteArray(socket, commandString);
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

        if (command.IsPropagated && ServerInfo.IsMaster)
        {
            foreach (var slaveSocket in SlaveServers.SlaveSockets)
            {
                Console.WriteLine($"Propagating command to slave '{slaveSocket.Key}'. Is Connected: '{slaveSocket.Value.Connected}'.");
                slaveSocket.Value.Send(Encoding.UTF8.GetBytes(commandString.Replace("\\r\\n", "\r\n")));
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