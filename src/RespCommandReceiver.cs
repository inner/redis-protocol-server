using codecrafters_redis.Enums;
using codecrafters_redis.RespCommands;

namespace codecrafters_redis;

public class RespCommandReceiver
{
    public string Receive(string respCommandString)
    {
        var respDataType = respCommandString.GetRespDataType();
        
        return respDataType switch
        {
            RespDataType.SimpleString => ExecuteSimpleString(respCommandString),
            RespDataType.SimpleError => ExecuteSimpleError(respCommandString),
            RespDataType.Integer => ExecuteInteger(respCommandString),
            RespDataType.BulkString => ExecuteBulkString(respCommandString),
            RespDataType.Array => ExecuteArray(respCommandString),
            _ => throw new ArgumentException("Invalid RESP data type.")
        };
    }

    private string ExecuteSimpleString(string respCommandString)
    {
        return "+PONG\\r\\n";
    }

    private string ExecuteSimpleError(string respCommandString)
    {
        return "-ERR unknown command 'foobar'\r\n";
    }

    private string ExecuteInteger(string respCommandString)
    {
        return ":1000\r\n";
    }

    private string ExecuteBulkString(string respCommandString)
    {
        return "$6\r\nfoobar\r\n";
    }

    private string ExecuteArray(string respCommandString)
    {
        var commandParts = respCommandString.Split("\\r\\n");
        var commandCount = int.Parse(commandParts[0].Replace("*", string.Empty));
        var respCommandType = commandParts[2].ToRespCommandType();

        return ExecuteCommand(respCommandType, commandCount, commandParts);
    }
    
    private string ExecuteCommand(RespCommandType commandType, int commandCount, string[] commandParts)
    {
        var className = $"codecrafters_redis.RespCommands.{commandType}";
        
        var type = Type.GetType(className);
        
        if (type == null)
        {
            throw new ArgumentException("Unknown RESP command.");
        }
        
        var command = (CommandBase)Activator.CreateInstance(type)!;

        return command.Execute(commandCount, commandParts);
    }
}