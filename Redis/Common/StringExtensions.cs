using System.Text;
using System.Text.Json;
using Redis.Commands.Common;

namespace Redis.Common;

public static class StringExtensions
{
    public static DataType GetRespDataType(this string respDataTypeString)
    {
        if (string.IsNullOrWhiteSpace(respDataTypeString))
        {
            throw new ArgumentException("Invalid RESP data type.");
        }

        return respDataTypeString[..1] switch
        {
            "+" => DataType.SimpleString,
            "-" => DataType.Error,
            ":" => DataType.Integer,
            "$" => DataType.BulkString,
            "*" => DataType.Array,
            _ => throw new ArgumentException("Invalid RESP data type.")
        };
    }

    public static RespType ToCommandType(this string respTypeString)
    {
        if (string.IsNullOrWhiteSpace(respTypeString))
        {
            throw new ArgumentException("Invalid RESP command type - empty or whitespace.");
        }

        if (Enum.TryParse<RespType>(respTypeString, true, out var respType))
        {
            return respType;
        }

        throw new ArgumentException($"Unknown client command: {respTypeString}");
    }

    public static CommandDetails BuildCommandDetails(this string resp)
    {
        var commandParts = resp.Split(Constants.VerbatimNewLine)
            .Where(x => !string.IsNullOrEmpty(x))
            .ToArray();

        return new CommandDetails
        {
            CommandCount = int.Parse(commandParts[0].Replace("*", string.Empty)),
            CommandParts = commandParts,
            Resp = resp,
            RespType = commandParts[2].ToCommandType(),
            FromTransaction = false
        };
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
    
    public static byte[] AsBytes(this string value)
    {
        return Encoding.UTF8.GetBytes(value);
    }
    
    public static string AsString(this byte[] value)
    {
        return Encoding.UTF8.GetString(value);
    }
}