using System.Text;

namespace codecrafters_redis.Common;

public static class RespBuilder
{
    public static string BuildRespArray(params string[] commands)
    {
        var sb = new StringBuilder();
        
        sb.Append($"*{commands.Length}\r\n");
        foreach (var command in commands)
        {
            sb.Append($"${command.Length}\r\n{command}\r\n");
        }

        return sb.ToString();
    }
    
    public static string BuildRespBulkString(string value)
    {
        return $"${value.Length}\r\n{value}\r\n";
    }
    
    public static string BuildRespInteger(int integer)
    {
        return $":{integer}\r\n";
    }
    
    public static string BuildRespError(string message)
    {
        return $"-ERR {message}\r\n";
    }
    
    public static byte[] AsBytes(this string resp)
    {
        return Encoding.UTF8.GetBytes(resp);
    }
}