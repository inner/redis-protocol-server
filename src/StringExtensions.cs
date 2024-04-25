using codecrafters_redis.Enums;

namespace codecrafters_redis;

public static class StringExtensions
{
    public static DataType GetRespDataType(this string respDataTypeString)
    {
        if (string.IsNullOrWhiteSpace(respDataTypeString))
        {
            throw new ArgumentException("Invalid RESP data type.");
        }
        
        return respDataTypeString.Substring(0, 1) switch
        {
            "+" => DataType.SimpleString,
            "-" => DataType.SimpleError,
            ":" => DataType.Integer,
            "$" => DataType.BulkString,
            "*" => DataType.Array,
            _ => throw new ArgumentException("Invalid RESP data type.")
        };
    }
    
    public static CommandType ToCommandType(this string respCommandTypeString)
    {
        if (string.IsNullOrWhiteSpace(respCommandTypeString))
        {
            throw new ArgumentException("Invalid RESP command type - empty or whitespace.");
        }

        if (Enum.TryParse<CommandType>(respCommandTypeString, true, out var respCommandType))
        {
            return respCommandType;
        }

        throw new ArgumentException($"Unknown client command: {respCommandTypeString}");
    }
}