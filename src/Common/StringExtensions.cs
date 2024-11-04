using System.Text.Json;
using codecrafters_redis.Commands;

namespace codecrafters_redis.Common;

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

    public static CommandDetails BuildCommandDetails(this string commandToExecute)
    {
        var commandParts = commandToExecute.Split(@"\r\n")
            .Where(x => !string.IsNullOrEmpty(x))
            .ToArray();

        return new CommandDetails
        {
            CommandCount = int.Parse(commandParts[0].Replace("*", string.Empty)),
            CommandParts = commandParts,
            CommandString = commandToExecute,
            CommandType = commandParts[2].ToCommandType()
        };
    }

    public static string ConvertStringToSimpleResp(this string? value)
    {
        string result;
        if (value == null)
        {
            result = "$-1";
        }
        else if (int.TryParse(value, out var numericResult))
        {
            result = $":{numericResult}";
        }
        else
        {
            result = $"+{value}";
        }

        return result;
    }

    public static string ConvertStringToStringResp(this string? value)
    {
        var result = value == null
            ? "$-1"
            : $"+{value}";

        return result;
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