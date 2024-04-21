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
            _ => throw new ArgumentException("Invalid RESP client command.")
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
        
        // var commandType = commandParts[0].GetRespCommandType();

        if (commandCount == 1 && string.Equals(commandParts[2], "ping",
                StringComparison.InvariantCultureIgnoreCase))
        {
            Console.WriteLine("Received PING command.");
            return "+PONG\r\n";
        }
        
        return "+PONG\r\n";
        
        // if (commandCount == 2 &&
        //          string.Equals(commandParts[2], "ping", StringComparison.InvariantCultureIgnoreCase))
        // {
        //     return $"${commandParts[4].Length}\\r\\n{commandParts[4]}\\r\\n";
        // }

        // return "*2\r\n$3\r\nfoo\r\n$3\r\nbar\r\n";
    }
}