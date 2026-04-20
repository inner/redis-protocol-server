using Redis.Common;

namespace Redis.Commands.Common;

public abstract class Base
{
    protected abstract string Name { get; }
    public abstract bool CanBePropagated { get; }
    protected virtual SupportedRoles SupportedRoles => SupportedRoles.Both;
    private bool TransactionStarted { get; set; }

    protected abstract Task<string> ExecuteCore(CommandContext commandContext);

    public async Task<string> Execute(CommandContext commandContext)
    {
        if (commandContext.Subscriptions.Count > 0)
        {
            var respType = commandContext.CommandDetails.RespType;
            if (!AllowedWhenInSubscribeMode.Contains(respType))
            {
                commandContext.Socket.Send(
                    RespBuilder.Error(
                        $"Can't execute '{respType.ToString().ToLower()}': " +
                        "only (P|S)SUBSCRIBE / (P|S)UNSUBSCRIBE / PING / QUIT / RESET are allowed in " +
                        "this context").AsBytes());
                
                return string.Empty;
            }
        }

        if (!IsSupportedOnCurrentNodeRole())
        {
            var role = ServerInfo.ServerRuntimeContext.IsMaster
                ? "master"
                : "replica";
            var command = commandContext.CommandDetails.RespType.ToString().ToUpperInvariant();
            var response = RespBuilder.Error($"Command '{command}' is not allowed on a {role} node.");
            commandContext.Socket.SendCommand(response);
            return string.Empty;
        }

        if (TransactionEnabled(commandContext))
        {
            return string.Empty;
        }

        return await ExecuteCore(commandContext);
    }

    protected static void SendIfNotFromTransaction(CommandContext commandContext, string response)
    {
        if (!commandContext.CommandDetails.FromTransaction)
        {
            commandContext.Socket.SendCommand(response);
        }
    }

    private bool IsSupportedOnCurrentNodeRole()
    {
        return (ServerInfo.ServerRuntimeContext.IsMaster, SupportedRoles) switch
        {
            (_, SupportedRoles.Both) => true,
            (true, SupportedRoles.MasterOnly) => true,
            (false, SupportedRoles.ReplicaOnly) => true,
            _ => false
        };
    }

    private bool TransactionEnabled(CommandContext commandContext)
    {
        if (!TransactionStarted)
        {
            TransactionStarted = commandContext.CommandQueue.Count != 0;
        }

        if (!TransactionStarted)
        {
            return false;
        }

        var commandTypesExcluded = new List<RespType>
        {
            RespType.Multi, RespType.Exec, RespType.Discard
        };

        var commandStrings = commandTypesExcluded.ConvertAll(c => c.ToString());
        if (commandStrings.Exists(c =>
                string.Equals(commandContext.CommandDetails.CommandParts[2], c,
                    StringComparison.InvariantCultureIgnoreCase)))
        {
            return false;
        }

        var commandType = commandContext.CommandDetails.CommandParts[2].ToCommandType();

        commandContext.CommandQueue.Add(
            new CommandQueueItem { RespType = commandType, Resp = commandContext.CommandDetails.Resp });

        commandContext.Socket.SendCommand(RespBuilder.SimpleString("QUEUED"));
        return true;
    }

    private static readonly RespType[] AllowedWhenInSubscribeMode =
    [
        RespType.Subscribe,
        RespType.Unsubscribe,
        RespType.Psubscribe,
        RespType.Punsubscribe,
        RespType.Ping,
        RespType.Quit
    ];
}
