using System.Net.Sockets;
using Redis.Commands.Common;
using Redis.Common;
using Redis.Receivers;

namespace Redis.Executors;

public class ArrayExecutor : IRespDataTypeExecutor
{
    // The connection reader hands over exactly one frame per call, so there
    // is no splitting of pipelined commands here.
    public async Task Execute(
        Socket socket, string resp, List<CommandQueueItem> commandQueue,
        List<string> subscriptions, ReceiverBase receiver, CommandSource source)
    {
        var commandDetails = resp.BuildCommandDetails();
        await receiver.ExecuteCommand(socket, commandDetails, commandQueue, subscriptions, source);
    }
}
