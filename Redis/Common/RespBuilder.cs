using System.Text;

namespace Redis.Common;

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
    
    public static string EmptyArray()
    {
        return "*0\r\n";
    }

    public static string BulkString(string value)
    {
        return $"${value.Length}\r\n{value}\r\n";
    }
    
    public static string SimpleString(string value)
    {
        return $"+{value}\r\n";
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
}