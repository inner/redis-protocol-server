using Redis.Cache;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Auth : Base
{
    protected override string Name => nameof(Auth);
    public override bool CanBePropagated => false;

    protected override async Task<string> ExecuteCore(CommandContext commandContext)
    {
        var commandParts = commandContext.CommandDetails.CommandParts;
        string resp;

        // AUTH <password> is the legacy form (used by `redis-cli -a`) and
        // authenticates as the default user; AUTH <username> <password> names
        // the user explicitly.
        var (username, password) = commandContext.CommandDetails.CommandCount switch
        {
            2 => ("default", commandParts[4]),
            3 => (commandParts[4], commandParts[6]),
            _ => (null, null)
        };

        if (username is null || password is null)
        {
            resp = RespBuilder.Error("wrong number of arguments for 'auth' command");
        }
        else if (PasswordHasher.VerifyPassword(password, DataCache.GetPasswordHash(username)))
        {
            DataCache.AddAuthenticatedConnection(username, commandContext.Socket);
            resp = RespBuilder.SimpleString("OK");
        }
        else
        {
            resp = RespBuilder.Error(
                "WRONGPASS invalid username-password pair or user is disabled.",
                includeErrPrefix: false);
        }

        commandContext.Socket.SendCommand(resp);
        return await Task.FromResult(resp);
    }
}
