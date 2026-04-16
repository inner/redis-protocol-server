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
        var username = commandParts[4];
        var password = commandParts[6];

        string resp;
        
        if (PasswordHasher.VerifyPassword(password, DataCache.GetPasswordHash(username)))
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
