namespace codecrafters_redis;

public static class RespClientCommandStringExtensions
{
    public static RespDataType GetRespDataType(this string respDataTypeString)
    {
        if (string.IsNullOrWhiteSpace(respDataTypeString))
        {
            throw new ArgumentException("Invalid RESP data type.");
        }
        
        return respDataTypeString.Substring(0, 1) switch
        {
            "+" => RespDataType.SimpleString,
            "-" => RespDataType.SimpleError,
            ":" => RespDataType.Integer,
            "$" => RespDataType.BulkString,
            "*" => RespDataType.Array,
            _ => throw new ArgumentException("Invalid RESP data type.")
        };
    }
    
    public static RespCommandType GetRespCommandType(this string respCommandTypeString)
    {
        if (string.IsNullOrWhiteSpace(respCommandTypeString))
        {
            throw new ArgumentException("Invalid RESP command type - empty or whitespace.");
        }

        if (string.Equals(respCommandTypeString, RespCommandType.Ping.ToString(),
                StringComparison.InvariantCultureIgnoreCase))
        {
            return RespCommandType.Ping;
        }

        if (string.Equals(respCommandTypeString, RespCommandType.Echo.ToString(),
                StringComparison.InvariantCultureIgnoreCase))
        {
            return RespCommandType.Echo;
        }

        throw new ArgumentException($"Unknown client command: {respCommandTypeString}");
    }
}