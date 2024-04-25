using System.Net.Sockets;
using codecrafters_redis.Commands;
using codecrafters_redis.Enums;

namespace codecrafters_redis;

public class Receiver
{
    public void Receive(Socket socket, string respCommandString)
    {
        respCommandString = respCommandString.Replace("\r\n", "\\r\\n");
        var respDataType = respCommandString.GetRespDataType();
        
        switch (respDataType)
        {
            case DataType.Array:
                ExecuteArray(socket, respCommandString);
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
    
    private void ExecuteArray(Socket socket, string respCommandString)
    {
        var commandParts = respCommandString.Split("\\r\\n");
        var commandCount = int.Parse(commandParts[0].Replace("*", string.Empty));
        var respCommandType = commandParts[2].ToRespCommandType();

        ExecuteCommand(socket, respCommandType, commandCount, commandParts);
    }
    
    private void ExecuteCommand(Socket socket, CommandType commandType, int commandCount, string[] commandParts)
    {
        var className = $"codecrafters_redis.Commands.{commandType}";
        var type = Type.GetType(className);
        
        if (type == null)
        {
            throw new ArgumentException("Unknown RESP command.");
        }
        
        var command = (Base)Activator.CreateInstance(type)!;
        command.Execute(socket, commandCount, commandParts);
    }

    private void ExecuteSimpleString()
    { }

    private void ExecuteSimpleError()
    { }

    private void ExecuteInteger()
    { }

    private void ExecuteBulkString()
    { }
}