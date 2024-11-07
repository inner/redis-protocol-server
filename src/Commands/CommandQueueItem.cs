using codecrafters_redis.Common;

namespace codecrafters_redis.Commands;

public class CommandQueueItem
{
    public CommandType CommandType { get; init; }
    public required string CommandString { get; init; }
}