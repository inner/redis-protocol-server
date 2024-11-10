using System.Text;

namespace codecrafters_redis.Common;

public static class RespBuilder
{
    public static string ArrayFromCommands(params string[] commands)
    {
        var sb = new StringBuilder($"*{commands.Length}\r\n");
        foreach (var command in commands)
        {
            sb.Append($"${command.Length}\r\n{command}\r\n");
        }

        return sb.ToString();
    }

    public static string BulkString(string value)
    {
        return $"${value.Length}\r\n{value}\r\n";
    }

    public static string Integer(long value)
    {
        return $":{value}\r\n";
    }

    public static string Error(string value)
    {
        return $"-ERR {value}\r\n";
    }
    
    public static string Null()
    {
        return "$-1\r\n";
    }

    public static byte[] AsBytes(this string value)
    {
        return Encoding.UTF8.GetBytes(value);
    }
}