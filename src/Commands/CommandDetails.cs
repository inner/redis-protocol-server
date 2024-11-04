using codecrafters_redis.Common;

namespace codecrafters_redis.Commands;

public class CommandDetails
{
    public int CommandCount { get; init; }
    public required string[] CommandParts { get; init; }
    public required string CommandString { get; init; }
    public required CommandType CommandType { get; init; }
    public required bool FromTransaction { get; set; }
}