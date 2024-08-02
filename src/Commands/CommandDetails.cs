using codecrafters_redis.Common;

namespace codecrafters_redis.Commands;

public class CommandDetails
{
    public int CommandCount { get; set; }
    public string[] CommandParts { get; set; }
    public string CommandString { get; set; }
    public CommandType CommandType { get; set; }
    public bool FromTransaction { get; set; }
}