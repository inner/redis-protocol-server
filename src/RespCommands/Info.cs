using System.Text;

namespace codecrafters_redis.RespCommands;

public class Info : CommandBase
{
    public override string Execute(int commandCount, string[] commandParts)
    {
        var dict = new Dictionary<string, string>
        {
            { "redis_version", "6.0.9" },
            { "redis_git_sha1", "00000000" },
            { "redis_git_dirty", "0" },
            { "redis_build_id", "b1b1b1b1b1b1b1b1" },
            { "redis_mode", "standalone" },
            { "os", "Linux 5.4.0-42-generic x86_64" },
            { "arch_bits", "64" },
            { "multiplexing_api", "epoll" },
            { "atomicvar_api", "atomic-builtin" },
            { "gcc_version", "9.3.0" },
            { "process_id", "1" },
            { "run_id", "" },
            { "tcp_port", "6379" },
            { "uptime_in_seconds", "0" },
            { "uptime_in_days", "0" },
            { "hz", "10" },
            { "configured_hz", "10" },
            { "lru_clock", "0" },
            { "executable", "/usr/local/bin/redis-server" },
            { "config_file", "/usr/local/etc/redis.conf" }
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