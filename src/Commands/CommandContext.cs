using System.Net.Sockets;
using codecrafters_redis.Receivers;

namespace codecrafters_redis.Commands;

public class CommandContext
{
    public required Socket Socket { get; init; }
    public required CommandDetails CommandDetails { get; init; }
    public required List<CommandQueueItem> CommandQueue { get; init; }
    public required ReceiverBase Receiver { get; init; }
    public required bool ReplicaConnection { get; init; }
}