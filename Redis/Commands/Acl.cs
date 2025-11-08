using System.Text;
using Redis.Cache;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Acl : Base
{
    protected override string Name => nameof(Acl);
    public override bool CanBePropagated => false;

    protected override async Task<string> OnMasterNodeExecute(CommandContext commandContext)
    {
        return await GenerateCommonResponse(commandContext);
    }

    protected override async Task<string> OnReplicaNodeExecute(CommandContext commandContext)
    {
        return await GenerateCommonResponse(commandContext);
    }

    private static async Task<string> GenerateCommonResponse(CommandContext commandContext)
    {
        string? resp;
        var commandParts = commandContext.CommandDetails.CommandParts;
        if (string.Equals(commandParts[4], "WHOAMI", StringComparison.InvariantCultureIgnoreCase))
        {
            if (ServerRuntimeContext.DefaultUserAuthenticated &&
                !DataCache.IsConnectionAuthenticated(commandContext.Socket))
            {
                resp = RespBuilder.Error("NOAUTH Authentication required.");
                commandContext.Socket.SendCommand(resp);
                return await Task.FromResult(resp);
            }

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
            
            DataCache.SetPassword(username, PasswordHasher.EncryptPassword(plaintextPassword));
            DataCache.AddAuthenticatedConnection(username, commandContext.Socket);
            
            resp = RespBuilder.SimpleString("OK");
        }
        else
        {
            resp = RespBuilder.Error("ACL command not implemented.");
        }

        commandContext.Socket.SendCommand(resp);
        return await Task.FromResult(resp);
    }

    public override Dictionary<string, Dictionary<string, string>> Docs()
    {
        return new Dictionary<string, Dictionary<string, string>>
        {
            {
                Name,
                new Dictionary<string, string>
                {
                    { "summary", "Manages the ACL (Access Control List) system." },
                    { "usage #1", "redis-cli" },
                    { "usage #2", "ACL LIST" },
                    { "usage #3", "ACL SETUSER myuser on >mypassword ~* +@all" },
                    { "usage #4", "ACL GETUSER myuser" },
                    { "usage #5", "ACL DELUSER myuser" }
                }
            }
        };
    }
}