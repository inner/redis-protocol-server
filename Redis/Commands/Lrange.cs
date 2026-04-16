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
        
        var sb = new StringBuilder(
            RespBuilder.InitArray(result.Count));
        
        foreach (var item in result)
        {
            sb.Append(RespBuilder.BulkString(item));
        }

        var resp = sb.ToString();
        commandContext.Socket.SendCommand(resp);

        return Task.FromResult(resp);
    }
}
