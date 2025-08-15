using System.Net.Sockets;
using Redis.Receivers;

namespace Redis.Commands.Common;

public class CommandContext
{
    public required Socket Socket { get; init; }
    public required CommandDetails CommandDetails { get; init; }
    public required List<CommandQueueItem> CommandQueue = new();
    public required List<string> Subscriptions = new();
    public required ReceiverBase Receiver { get; init; }
    public required bool ReplicaConnection { get; init; }
}