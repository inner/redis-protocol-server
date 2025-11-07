using System.Text;
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
        string? resp = null;
        var commandParts = commandContext.CommandDetails.CommandParts;
        if (string.Equals(commandParts[4], "WHOAMI", StringComparison.InvariantCultureIgnoreCase))
        {
            resp = RespBuilder.BulkString("default");
        }
        else if (string.Equals(commandParts[4], "GETUSER", StringComparison.InvariantCultureIgnoreCase))
        {
            var sb = new StringBuilder(RespBuilder.InitArray(2));
            sb.Append(RespBuilder.BulkString("flags"));
            sb.Append(RespBuilder.EmptyArray());
            resp = sb.ToString();
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