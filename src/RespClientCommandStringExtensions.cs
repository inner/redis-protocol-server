namespace codecrafters_redis;

public static class RespClientCommandStringExtensions
{
    public static RespClientCommandType GetRespClientCommandType(this string clientCommandString)
    {
        if (string.IsNullOrWhiteSpace(clientCommandString))
        {
            throw new ArgumentException("Invalid RESP client command.");
        }
        
        return clientCommandString.Substring(0, 1) switch
        {
            "+" => RespClientCommandType.SimpleString,
            "-" => RespClientCommandType.SimpleError,
            ":" => RespClientCommandType.Integer,
            "$" => RespClientCommandType.BulkString,
            "*" => RespClientCommandType.Array,
            _ => throw new ArgumentException("Invalid RESP client command.")
        };
    }
}

public enum RespClientCommandType
{
    SimpleString,
    SimpleError,
    Integer,
    BulkString,
    Array
}