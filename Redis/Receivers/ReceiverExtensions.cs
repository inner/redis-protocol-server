using System.Net.Sockets;
using Redis.Commands.Common;

namespace Redis.Receivers;

public static class ReceiverExtensions
{
    public static async Task<string> ExecuteCommand(
        this ReceiverBase receiver,
        Socket socket,
        CommandDetails commandDetails,
        List<CommandQueueItem> commandQueue,
        List<string> subscriptions)
    {
        var className = $"Redis.Commands.{commandDetails.RespType}";

        var type = Type.GetType(className);
        if (type == null)
        {
            throw new ArgumentException("Unknown RESP command.");
        }

        var command = (Base)Activator.CreateInstance(type)!;

        var commandContext = new CommandContext
        {
            Socket = socket,
            CommandDetails = commandDetails,
            CommandQueue = commandQueue,
            Subscriptions = subscriptions,
            Receiver = receiver
        };

        var result = await command.Execute(commandContext);

        if (!ShouldReplicateCommand(command, commandDetails))
        {
            return result;
        }

        await ServerRuntimeContext.ExecuteOnReplicas(commandDetails.Resp);
        return result;
    }

    private static bool ShouldReplicateCommand(Base command, CommandDetails commandDetails)
    {
        // do not replicate the command if:
        // - the server is not a master
        // - the command cannot be propagated
        // - the command is an ACK response

        return ServerInfo.ServerRuntimeContext.IsMaster &&
               command.CanBePropagated &&
               !commandDetails.Resp.Contains(@"$3\r\nACK\r\n");
    }
}