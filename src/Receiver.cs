using codecrafters_redis.Enums;
using codecrafters_redis.RespCommands;

namespace codecrafters_redis;

public class Receiver
{
    public string Receive(string respCommandString)
    {
        respCommandString = respCommandString.Replace("\r\n", "\\r\\n");
        var respDataType = respCommandString.GetRespDataType();
        
        return respDataType switch
        {
            DataType.SimpleString => ExecuteSimpleString(),
            DataType.SimpleError => ExecuteSimpleError(),
            DataType.Integer => ExecuteInteger(),
            DataType.BulkString => ExecuteBulkString(),
            DataType.Array => ExecuteArray(respCommandString),
            _ => throw new ArgumentException("Invalid RESP data type.")
        };
    }

    private string ExecuteSimpleString()
    {
        return "+PONG\\r\\n";
    }

    private string ExecuteSimpleError()
    {
        return "-ERR unknown command 'foobar'\r\n";
    }

    private string ExecuteInteger()
    {
        return ":1000\r\n";
    }

    private string ExecuteBulkString()
    {
        return "$6\r\nfoobar\r\n";
    }

    private string ExecuteArray(string respCommandString)
    {
        var commandParts = respCommandString.Split("\\\\r\\\\n");
        var commandCount = int.Parse(commandParts[0].Replace("*", string.Empty));
        var respCommandType = commandParts[2].ToRespCommandType();

        return ExecuteCommand(respCommandType, commandCount, commandParts);
    }
    
    private string ExecuteCommand(CommandType commandType, int commandCount, string[] commandParts)
    {
        var className = $"codecrafters_redis.RespCommands.{commandType}";
        
        var type = Type.GetType(className);
        
        if (type == null)
        {
            throw new ArgumentException("Unknown RESP command.");
        }
        
        var command = (Base)Activator.CreateInstance(type)!;

        return command.Execute(commandCount, commandParts);
    }
}