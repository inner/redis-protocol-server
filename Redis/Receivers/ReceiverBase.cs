using System.Net.Sockets;
using Redis.Commands.Common;
using Redis.Common;
using Redis.Executors;

namespace Redis.Receivers;

public class ReceiverBase
{
    public virtual async Task Receive(Socket socket, string commandString, List<CommandQueueItem> commandQueue)
    {
        try
        {
            var respDataType = commandString.GetRespDataType();
            if (ExecutorRegistry.Executors.TryGetValue(respDataType, out var executor))
            {
                await executor.Execute(socket, commandString, commandQueue, this);
            }
            else
            {
                throw new ArgumentException("Invalid data type.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Some exception occured: {ex.Message}, stack: {ex.StackTrace}");
            throw;
        }
    }
}