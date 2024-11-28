using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

// spec: https://redis.io/docs/latest/commands/exists/
public class Exists : Base
{
    protected override string Name => nameof(Exists);
    public override bool CanBePropagated => false;

    public override Dictionary<string, Dictionary<string, string>> Docs()
    {
        return new()
        {
            {
                Name,
                new()
                {
                    {"summary", "Determines whether one or more keys exist."},
                    {"usage", "redis-cli EXISTS mykey1"}
                }
            }
        };
    }

    protected override async Task<string> OnMasterNodeExecute(CommandContext commandContext)
    {
        return await GenerateCommonResponse(commandContext);
    }

    protected override async Task<string> OnReplicaNodeExecute(CommandContext commandContext)
    {
        return await GenerateCommonResponse(commandContext);
    }

    private static Task<string> GenerateCommonResponse(CommandContext commandContext)
    {
        // hardcoding the response to 1
        var result = RespBuilder.Integer(1);
        commandContext.Socket.SendCommand(result);
        return Task.FromResult(result);
    }
}