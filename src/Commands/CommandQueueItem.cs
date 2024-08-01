using codecrafters_redis.Common;

namespace codecrafters_redis.Commands;

public class CommandQueueItem
{
    public CommandType CommandType { get; set; }
    public string CommandString { get; set; }
}