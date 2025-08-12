using Redis.Cache;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Rpush : Base
{
    protected override string Name => nameof(Rpush);
    public override bool CanBePropagated => true;

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
        var key = commandContext.CommandDetails.CommandParts[4];
        var values = commandContext.CommandDetails.CommandParts.Skip(6)
            .Where((_, i) => i % 2 == 0)
            .ToArray();

        var result = RespBuilder.Integer(DataCache.Rpush(key, values));

        if (!commandContext.ReplicaConnection)
        {
            commandContext.Socket.SendCommand(result);
        }

        return Task.FromResult(result);
    }

    public override Dictionary<string, Dictionary<string, string>> Docs()
    {
        return new()
        {
            {
                Name,
                new()
                {
                    { "summary", "Appends one or more elements to a list. Creates the key if it doesn't exist." },
                    { "usage #1", "RPUSH mylist \"hello\"" },
                    { "usage #2", "RPUSH mylist \"world\"" },
                    { "usage #3", "LRANGE mylist 0 -1" }
                }
            }
        };
    }
}