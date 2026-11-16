using System.Text;
using Redis.Cache;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Acl : Base
{
    protected override string Name => nameof(Acl);
    public override bool CanBePropagated => false;

    protected override async Task<string> ExecuteCore(CommandContext commandContext)
    {
        string? resp;
        var commandParts = commandContext.CommandDetails.CommandParts;
        if (string.Equals(commandParts[4], "WHOAMI", StringComparison.InvariantCultureIgnoreCase))
        {
            // Unauthenticated connections never get here: Base.Execute
            // rejects them with NOAUTH.
            resp = RespBuilder.BulkString(
                DataCache.GetAuthenticatedUsername(commandContext.Socket) ?? "default");
        }
        else if (string.Equals(commandParts[4], "GETUSER", StringComparison.InvariantCultureIgnoreCase))
        {
            var username = commandParts[6];
            var passwordHash = DataCache.GetPasswordHash(username);

            var sb = new StringBuilder(RespBuilder.InitArray(4));

            // flags
            sb.Append(RespBuilder.BulkString("flags"));
            if (passwordHash != null)
            {
                sb.Append(RespBuilder.InitArray(0));
            }
            else
            {
                sb.Append(RespBuilder.InitArray(1));
                sb.Append(RespBuilder.BulkString("nopass"));
            }

            // passwords
            sb.Append(RespBuilder.BulkString("passwords"));
            if (passwordHash != null)
            {
                sb.Append(RespBuilder.InitArray(1));
                sb.Append(RespBuilder.BulkString(passwordHash));
            }
            else
            {
                sb.Append(RespBuilder.InitArray(0));
            }

            resp = sb.ToString();
        }
        else if (string.Equals(commandParts[4], "SETUSER", StringComparison.InvariantCultureIgnoreCase))
        {
            var username = commandParts[6];
            var plaintextPassword = commandParts[8];
            if (plaintextPassword.StartsWith('>'))
            {
                plaintextPassword = plaintextPassword[1..];
            }
            
            // The calling connection keeps whatever login it has, as in Redis.
            DataCache.SetPassword(username, PasswordHasher.EncryptPassword(plaintextPassword));
            
            resp = RespBuilder.SimpleString("OK");
        }
        else
        {
            resp = RespBuilder.Error("ACL command not implemented.");
        }

        Reply(commandContext, resp);
        return await Task.FromResult(resp);
    }
}
