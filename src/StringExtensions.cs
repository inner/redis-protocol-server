using System.Text.Json;
using codecrafters_redis.Enums;

namespace codecrafters_redis;

public static class StringExtensions
{
    public static DataTypes GetRespDataType(this string respDataTypeString)
    {
        if (string.IsNullOrWhiteSpace(respDataTypeString))
        {
            throw new ArgumentException("Invalid RESP data type.");
        }
        
        return respDataTypeString.Substring(0, 1) switch
        {
            "+" => DataTypes.SimpleString,
            "-" => DataTypes.SimpleError,
            ":" => DataTypes.Integer,
            "$" => DataTypes.BulkString,
            "*" => DataTypes.Array,
            _ => throw new ArgumentException("Invalid RESP data type.")
        };
    }
    
    public static CommandTypes ToCommandType(this string respCommandTypeString)
    {
        if (string.IsNullOrWhiteSpace(respCommandTypeString))
        {
            throw new ArgumentException("Invalid RESP command type - empty or whitespace.");
        }

        if (Enum.TryParse<CommandTypes>(respCommandTypeString, true, out var respCommandType))
        {
            return respCommandType;
        }

        throw new ArgumentException($"Unknown client command: {respCommandTypeString}");
    }
    
    public static T? Deserialize<T>(this string value)
    {
        T? result = default;

        try
        {
            result = JsonSerializer.Deserialize<T>(value);
        }
        catch (Exception)
        {
            // ignored
        }

        return result;
    }
}