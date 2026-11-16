using Redis.Aof;
using Redis.Cache;
using Redis.Common;

namespace Redis.Commands.Common;

public abstract class Base
{
    protected abstract string Name { get; }
    public abstract bool CanBePropagated { get; }
    protected virtual SupportedRoles SupportedRoles => SupportedRoles.Both;
    private bool TransactionStarted { get; set; }

    // True once ExecuteCore ran, i.e. the command was not rejected (auth,
    // role, subscribe mode) or queued by MULTI. Only executed commands are
    // propagated to replicas.
    public bool Executed { get; private set; }

    protected abstract Task<string> ExecuteCore(CommandContext commandContext);

    public async Task<string> Execute(CommandContext commandContext)
    {
        if (commandContext.Subscriptions.Count > 0)
        {
            var respType = commandContext.CommandDetails.RespType;
            if (!AllowedWhenInSubscribeMode.Contains(respType))
            {
                var error = RespBuilder.Error(
                    $"Can't execute '{respType.ToString().ToLower()}': " +
                    "only (P|S)SUBSCRIBE / (P|S)UNSUBSCRIBE / PING / QUIT / RESET are allowed in " +
                    "this context");

                Reply(commandContext, error);
                return error;
            }
        }

        // Once the default user has a password, a connection must AUTH before
        // running anything else. Replicas authenticate with --masterauth; the
        // replication stream a replica receives from its master is trusted.
        if (ServerRuntimeContext.DefaultUserAuthenticated &&
            !commandContext.IsReplicationStream &&
            !AllowedWithoutAuthentication.Contains(commandContext.CommandDetails.RespType) &&
            !DataCache.IsConnectionAuthenticated(commandContext.Socket))
        {
            var error = RespBuilder.Error("NOAUTH Authentication required.", includeErrPrefix: false);
            Reply(commandContext, error);
            return error;
        }

        if (!IsSupportedOnCurrentNodeRole())
        {
            var role = ServerInfo.ServerRuntimeContext.IsMaster
                ? "master"
                : "replica";
            var command = commandContext.CommandDetails.RespType.ToString().ToUpperInvariant();
            var response = RespBuilder.Error($"Command '{command}' is not allowed on a {role} node.");
            Reply(commandContext, response);
            return response;
        }

        if (TransactionEnabled(commandContext))
        {
            return string.Empty;
        }

        // Write commands are logged once they are known to run (not merely
        // queued by MULTI) and before they reply, so appendfsync=always holds.
        // Commands inside EXEC pass through here individually.
        // TODO: commands that end up failing (e.g. INCR on a non-integer) are
        // still logged; replaying them fails the same way, so state is unaffected.
        if (AofManager.ShouldLog(commandContext.CommandDetails.RespType, CanBePropagated))
        {
            AofManager.Append(commandContext.CommandDetails.Resp);
        }

        Executed = true;
        return await ExecuteCore(commandContext);
    }

    // Sends a command's reply to its client. Two callers get no reply:
    // - EXEC runs queued commands with FromTransaction set and collects their
    //   return values into one array reply (also used by AOF replay);
    // - a replica applying its master's replication stream, since a master
    //   expects no replies except to REPLCONF GETACK (sent directly).
    // Every command reply goes through here, and each ExecuteCore returns the
    // same string, so EXEC can include it.
    protected static void Reply(CommandContext commandContext, string response)
    {
        if (!commandContext.CommandDetails.FromTransaction && !commandContext.IsReplicationStream)
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
            RespType.Multi, RespType.Exec, RespType.Discard, RespType.Watch
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

        Reply(commandContext, RespBuilder.SimpleString("QUEUED"));
        return true;
    }

    private static readonly RespType[] AllowedWithoutAuthentication =
    [
        RespType.Auth,
        RespType.Quit
    ];

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
