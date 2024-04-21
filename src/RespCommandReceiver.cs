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
        var respCommandType = commandParts[2].GetRespCommandType();
        
        return respCommandType switch
        {
            RespCommandType.Ping => new Ping().Execute(commandCount, commandParts),
            RespCommandType.Echo => new Echo().Execute(commandCount, commandParts),
            _ => throw new ArgumentException("Unknown RESP command type.")
        };
    }
}