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
            resp = RespBuilder.BulkString("default");
        }
        else if (string.Equals(commandParts[4], "GETUSER", StringComparison.InvariantCultureIgnoreCase))
        {
            var username = commandParts[6];
            var sb = new StringBuilder(RespBuilder.InitArray(4));
            sb.Append(RespBuilder.BulkString("flags"));
            if (string.Equals(username, "default", StringComparison.InvariantCultureIgnoreCase))
            {
                sb.Append(RespBuilder.InitArray(1));
                sb.Append(RespBuilder.BulkString("nopass"));
            }
            sb.Append(RespBuilder.BulkString("passwords"));
            var passwordHash = DataCache.GetPassword(username);
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
            
            var sha256 = System.Security.Cryptography.SHA256.Create();
            var passwordBytes = Encoding.UTF8.GetBytes(plaintextPassword);
            var hashBytes = sha256.ComputeHash(passwordBytes);
            var passwordHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            DataCache.SetPassword(username, passwordHash);
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
        return new()
        {
            {
                Name,
                new()
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