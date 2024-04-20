namespace codecrafters_redis;

public class RespClientCommandExecutor
{
    public string Execute(RespClientCommandType respClientCommandType, string respCommandString)
    {
        return respClientCommandType switch
        {
            RespClientCommandType.SimpleString => ExecuteSimpleString(respCommandString),
            RespClientCommandType.SimpleError => ExecuteSimpleError(respCommandString),
            RespClientCommandType.Integer => ExecuteInteger(respCommandString),
            RespClientCommandType.BulkString => ExecuteBulkString(respCommandString),
            RespClientCommandType.Array => ExecuteArray(respCommandString),
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
        
        return "*2\r\\n$3\\r\\nfoo\\r\\n$3\\r\\nbar\\r\\n";
    }
}