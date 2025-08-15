using System.Net.Sockets;
using Redis.Commands.Common;
using Redis.Receivers;

namespace Redis.Executors;

public interface IRespDataTypeExecutor
{
    Task Execute(Socket socket, string resp, List<CommandQueueItem> commandQueue, List<string> subscriptions,
        ReceiverBase receiver);
}