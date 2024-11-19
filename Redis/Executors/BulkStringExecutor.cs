using System.Net.Sockets;
using Redis.Commands.Common;
using Redis.Common;
using Redis.Receivers;

namespace Redis.Executors;

public class BulkStringExecutor : IRespDataTypeExecutor
{
    public Task Execute(Socket socket, string resp, List<CommandQueueItem> commandQueue, ReceiverBase receiver)
    {
        socket.SendCommand(RespBuilder.ArrayFromCommands("hardcoded", "response"));
        return Task.CompletedTask;
    }
}