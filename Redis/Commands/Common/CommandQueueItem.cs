using Redis.Common;

namespace Redis.Commands.Common;

public class CommandQueueItem
{
    public CommandType CommandType { get; init; }
    public required string CommandString { get; init; }
}