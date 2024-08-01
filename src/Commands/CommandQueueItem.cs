using codecrafters_redis.Enums;

namespace codecrafters_redis.Commands;

public class CommandQueueItem
{
    public CommandType CommandType { get; set; }
    public string CommandString { get; set; }
}