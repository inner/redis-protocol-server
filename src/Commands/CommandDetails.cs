using codecrafters_redis.Common;

namespace codecrafters_redis.Commands;

public class CommandDetails
{
    public int CommandCount { get; init; }
    public required string[] CommandParts { get; init; }
    public required string CommandString { get; init; }
    public CommandType CommandType { get; init; }
    public bool FromTransaction { get; set; }
}