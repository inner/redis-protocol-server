using System.Text;
using Redis.Cache;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Lrange : Base
{
    protected override string Name => nameof(Lrange);
    public override bool CanBePropagated => false;

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
        var commands = commandContext.CommandDetails.CommandParts;

        var key = commands[4];
        var start = int.Parse(commands[6]);
        var end = int.Parse(commands[8]);

        var result = DataCache.Lrange(key, start, end);
        var sb = new StringBuilder(RespBuilder.InitArray(result.Count));
        
        foreach (var item in result)
        {
            sb.Append(RespBuilder.SimpleString(item));
        }

        var resp = sb.ToString();

        if (!commandContext.ReplicaConnection)
        {
            commandContext.Socket.SendCommand(resp);
        }

        return Task.FromResult(resp);
    }

    public override Dictionary<string, Dictionary<string, string>> Docs()
    {
        return new()
        {
            {
                Name,
                new()
                {
                    { "summary", "Returns a range of elements from a list." },
                    { "usage #1", "LRANGE mylist 0 -1" },
                    { "usage #2", "LRANGE mylist 0 1" },
                    { "usage #3", "LRANGE mylist 1 2" }
                }
            }
        };
    }
}