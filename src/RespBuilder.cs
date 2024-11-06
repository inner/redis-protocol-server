using System.Text;

namespace codecrafters_redis;

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
}