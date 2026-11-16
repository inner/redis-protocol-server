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
            // A master expects no replies on the replication stream.
            if (source == CommandSource.ReplicationMaster)
            {
                Console.WriteLine($"Failed to apply replicated command: {ex.Message}");
                return;
            }

            socket.SendCommand(RespBuilder.Error(ex.Message));
        }
    }
}
