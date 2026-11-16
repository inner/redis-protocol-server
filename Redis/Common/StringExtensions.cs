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

    // Turns one RESP array frame into the flat part list commands index into:
    // ["*3", "$3", "SET", "$3", "key", "$5", "value"]. Values are cut by their
    // declared length, so they may contain "\r\n" or be empty.
    public static CommandDetails BuildCommandDetails(this string resp)
    {
        var position = 0;

        string ReadLine()
        {
            var end = resp.IndexOf(Constants.NewLine, position, StringComparison.Ordinal);
            if (end < 0)
            {
                throw new ArgumentException("Incomplete RESP command.");
            }

            var line = resp[position..end];
            position = end + Constants.NewLine.Length;
            return line;
        }

        var header = ReadLine();
        if (!header.StartsWith('*') || !int.TryParse(header.AsSpan(1), out var count) || count < 1)
        {
            throw new ArgumentException("Expected a RESP array command.");
        }

        var commandParts = new List<string> { header };
        for (var i = 0; i < count; i++)
        {
            var lengthLine = ReadLine();
            if (!lengthLine.StartsWith('$') || !int.TryParse(lengthLine.AsSpan(1), out var length) ||
                length < 0 || position + length > resp.Length)
            {
                throw new ArgumentException("Expected a RESP bulk string argument.");
            }

            commandParts.Add(lengthLine);
            commandParts.Add(resp.Substring(position, length));
            position += length + Constants.NewLine.Length;
        }

        return new CommandDetails
        {
            CommandCount = count,
            CommandParts = commandParts.ToArray(),
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
    
    // The wire encoding is Latin-1, not UTF-8: each byte is one char, so RESP
    // lengths (byte counts) match string lengths and binary values such as
    // bitmaps round-trip unchanged. UTF-8 text from clients is simply carried
    // as its raw bytes.
    public static byte[] AsBytes(this string value)
    {
        return Encoding.Latin1.GetBytes(value);
    }
    
    public static string AsString(this byte[] value)
    {
        return Encoding.Latin1.GetString(value);
    }
}