using System.Text;

namespace codecrafters_redis.Commands;

public class Info : Base
{
    public override string Execute(int commandCount, string[] commandParts)
    {
        // var dict = new Dictionary<string, string>();
        
        // if (ServerInfo.IsMaster)
        // {
        //     dict.Add("master_replid", GenerateRandomReplId());
        //     dict.Add("master_repl_offset", "0");
        // }

        var dict = new Dictionary<string, string>
        {
            { "master_replid", GenerateRandomReplId() },
            { "master_repl_offset", "0" },
            { "role", ServerInfo.IsMaster ? "master" : "slave" },
            { "connected_slaves", "0" },
            { "second_repl_offset", "-1" },
            { "repl_backlog_active", "0" },
            { "repl_backlog_size", "1048576" },
            { "repl_backlog_first_byte_offset", "0" },
            { "repl_backlog_histlen", string.Empty }
        };

        var sb = new StringBuilder();

        foreach (var (key, value) in dict)
        {
            var valueString = $"{key}:{value}";
            sb.Append($"${valueString.Length}\r\n{valueString}\r\n");
        }

        return sb.ToString();
    }

    private string GenerateRandomReplId()
    {
        var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        var result = new string(
            Enumerable.Repeat(chars, 40)
                .Select(s => s[random.Next(s.Length)])
                .ToArray()
        );

        return result;
    }
}