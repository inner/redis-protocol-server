using Redis.Common;

namespace Redis.Commands.Common;

public class CommandQueueItem
{
    public RespType RespType { get; init; }
    public required string Resp { get; init; }
}