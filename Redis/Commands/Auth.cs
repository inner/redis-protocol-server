using Redis.Cache;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Auth : Base
{
    protected override string Name => nameof(Auth);
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

    public override Dictionary<string, Dictionary<string, string>> Docs()
    {
        return new Dictionary<string, Dictionary<string, string>>
        {
            {
                "AUTH", new Dictionary<string, string>
                {
                    { "summary", "Authenticate to the server" },
                    { "since", "1.0.0" },
                    { "group", "connection" },
                    { "complexity", "1" },
                    { "arity", "2..N" },
                    { "container_commands", "false" },
                    { "history", "AUTH was added in Redis 1.0.0." },
                    {
                        "notes",
                        "The AUTH command is used to authenticate a client connection to the Redis server using a password."
                    }
                }
            }
        };
    }
}