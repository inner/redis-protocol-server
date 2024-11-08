using System.Text;

namespace codecrafters_redis.Common;

public static class RespBuilder
{
    public static string BuildRespArray(params object[] commands)
    {
        var sb = new StringBuilder();

        // Append array header with the number of elements
        sb.Append($"*{commands.Length}\r\n");

        foreach (var command in commands)
        {
            if (command is string str)
            {
                // Encode non-null strings as bulk strings
                sb.Append($"${str.Length}\r\n{str}\r\n");
            }
            else if (command is int integer)
            {
                // Encode integers with colon prefix
                sb.Append($":{integer}\r\n");
            }
            else if (command == null)
            {
                // Encode null elements as `$-1\r\n`
                sb.Append("$-1\r\n");
            }
            else
            {
                throw new ArgumentException("Unsupported data type in RESP array");
            }
        }

        return sb.ToString();
    }

    public static string BuildRespBulkString(string value)
    {
        return $"${value.Length}\r\n{value}\r\n";
    }

    public static string BuildRespInteger(long value)
    {
        return $":{value}\r\n";
    }

    public static string BuildRespError(string value)
    {
        return $"-ERR {value}\r\n";
    }

    public static byte[] AsBytes(this string value)
    {
        return Encoding.UTF8.GetBytes(value);
    }
}