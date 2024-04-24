using codecrafters_redis.Enums;

namespace codecrafters_redis;

public static class StringExtensions
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
    
    public static RespCommandType ToRespCommandType(this string respCommandTypeString)
    {
        if (string.IsNullOrWhiteSpace(respCommandTypeString))
        {
            throw new ArgumentException("Invalid RESP command type - empty or whitespace.");
        }

        if (Enum.TryParse<RespCommandType>(respCommandTypeString, true, out var respCommandType))
        {
            return respCommandType;
        }

        throw new ArgumentException($"Unknown client command: {respCommandTypeString}");
    }
}