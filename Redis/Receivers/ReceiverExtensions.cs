using System.Net.Sockets;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Receivers;

public static class ReceiverExtensions
{
    public static async Task<string> ExecuteCommand(
        this ReceiverBase receiver,
        Socket socket,
        CommandDetails commandDetails,
        List<CommandQueueItem> commandQueue,
        List<string> subscriptions,
        CommandSource source)
    {
        var command = CreateCommand(commandDetails.RespType);

        var commandContext = new CommandContext
        {
            Socket = socket,
            Source = source,
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

    public static Base CreateCommand(RespType respType)
    {
        var type = Type.GetType($"Redis.Commands.{respType}")
                   ?? throw new ArgumentException("Unknown RESP command.");

        return (Base)Activator.CreateInstance(type)!;
    }

    // Propagate write commands from a master once they have actually run.
    // Not propagated:
    // - commands rejected before running, or queued by MULTI (not Executed);
    // - commands run by EXEC (FromTransaction): EXEC propagates the whole
    //   transaction as one MULTI ... EXEC block instead.
    private static bool ShouldReplicateCommand(Base command, CommandDetails commandDetails)
    {
        return ServerInfo.ServerRuntimeContext.IsMaster &&
               command.CanBePropagated &&
               command.Executed &&
               !commandDetails.FromTransaction;
    }
}
