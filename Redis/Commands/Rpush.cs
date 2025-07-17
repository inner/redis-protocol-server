using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Rpush : Base
{
    protected override string Name => nameof(Rpush);
    public override bool CanBePropagated => false;

    protected override async Task<string> OnMasterNodeExecute(CommandContext commandContext)
    {
        return await GenerateCommonResponse();
    }

    protected override async Task<string> OnReplicaNodeExecute(CommandContext commandContext)
    {
        return await GenerateCommonResponse();
    }

    private static Task<string> GenerateCommonResponse()
    {
        var result = RespBuilder.Integer(1);
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
                    {"summary", "Appends one or more elements to a list. Creates the key if it doesn't exist."},
                    {"usage #1", "RPUSH mylist \"hello\""},
                    {"usage #2", "RPUSH mylist \"world\""},
                    {"usage #3", "LRANGE mylist 0 -1"}
                }
            }
        };
    }
}