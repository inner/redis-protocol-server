using System.Text;

namespace codecrafters_redis.RespCommands;

public class Info : Base
{
    public override string Execute(int commandCount, string[] commandParts)
    {
        var dict = new Dictionary<string, string>
        {
            { "role", ServerInfo.IsMaster ? "master" : "slave" },
            { "connected_slaves", "0" },
            { "master_replid", "8371b4fb1155b71f4a04d3e1bc3e18c4a990aeeb" },
            { "master_repl_offset", "0" },
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
}