using codecrafters_redis.Enums;

namespace codecrafters_redis.Commands;

public class CommandQueueItem
{
    public CommandTypes CommandType { get; set; }
    public string CommandString { get; set; }
}