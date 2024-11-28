using Redis.Cache;
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
                    {"usage #1", "redis-cli SET mykey1 myval1"},
                    {"usage #2", "redis-cli SET mykey2 myval2"},
                    {"usage #3", "redis-cli EXISTS mykey1 mykey2 nosuchkey"}
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
        var keys = commandContext.CommandDetails.CommandParts[3..]
            .Where(x => !x.StartsWith("$"))
            .Distinct();

        var count = DataCache.CountKeys(keys.ToArray());
        var result = RespBuilder.Integer(count);
        commandContext.Socket.SendCommand(result);
        return Task.FromResult(result);
    }
}