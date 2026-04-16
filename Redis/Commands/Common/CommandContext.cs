using System.Net.Sockets;
using Redis.Common;
using Redis.Receivers;

namespace Redis.Commands.Common;

public class CommandContext
{
    public required Socket Socket { get; init; }
    public string ClientId => Socket.RemoteEndPoint!.ToString()!;
    public required CommandSource Source { get; init; }
    public bool IsReplicationStream => Source == CommandSource.ReplicationMaster;
    public required CommandDetails CommandDetails { get; init; }
    public required List<CommandQueueItem> CommandQueue = new();
    public required List<string> Subscriptions = new();
    public required ReceiverBase Receiver { get; init; }
}
