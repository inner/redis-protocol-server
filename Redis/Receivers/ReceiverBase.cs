using System.Net.Sockets;
using Redis.Commands.Common;
using Redis.Common;
using Redis.Executors;

namespace Redis.Receivers;

public class ReceiverBase
{
    public virtual async Task Receive(Socket socket, string resp, List<CommandQueueItem> commandQueue,
        List<string> subscriptions, CommandSource source)
    {
        try
        {
            var respDataType = resp.GetRespDataType();

            if (ExecutorRegistry.Executors.TryGetValue(respDataType, out var executor))
            {
                await executor.Execute(socket, resp, commandQueue, subscriptions, this, source);
            }
            else
            {
                throw new ArgumentException("Unknown RESP data type.");
            }
        }
        catch (Exception ex)
        {
            socket.SendCommand(RespBuilder.Error(ex.Message));
        }
    }
}
