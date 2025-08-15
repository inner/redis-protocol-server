using System.Net.Sockets;
using Redis.Commands.Common;
using Redis.Receivers;

namespace Redis.Executors;

public class SimpleStringExecutor : IRespDataTypeExecutor
{
    public Task Execute(Socket socket, string resp, List<CommandQueueItem> commandQueue,
        List<string> subscribedChannels, ReceiverBase receiver)
    {
        return Task.CompletedTask;
    }
}