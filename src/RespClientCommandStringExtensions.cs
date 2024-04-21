namespace codecrafters_redis;

public static class RespClientCommandStringExtensions
{
    public static RespDataType GetRespDataType(this string clientCommandString)
    {
        if (string.IsNullOrWhiteSpace(clientCommandString))
        {
            throw new ArgumentException("Invalid RESP client command.");
        }
        
        return clientCommandString.Substring(0, 1) switch
        {
            "+" => RespDataType.SimpleString,
            "-" => RespDataType.SimpleError,
            ":" => RespDataType.Integer,
            "$" => RespDataType.BulkString,
            "*" => RespDataType.Array,
            _ => throw new ArgumentException("Invalid RESP client command.")
        };
    }
}

public enum RespDataType
{
    SimpleString,
    SimpleError,
    Integer,
    BulkString,
    Array
}