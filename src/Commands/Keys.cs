using System.Text;
using codecrafters_redis.Cache;
using codecrafters_redis.Commands.Common;
using codecrafters_redis.Common;

namespace codecrafters_redis.Commands;

public class Keys : Base
{
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
        var keys = DataCache.GetKeys(commandContext.CommandDetails.CommandParts[4]);
        if (keys.Count == 0)
        {
            commandContext.Socket.SendCommand(RespBuilder.EmptyArray());
        }

        var sb = new StringBuilder($"*{keys.Count}\r\n");
        foreach (var key in keys)
        {
            sb.Append($"${key.Length}\r\n{key}\r\n");
        }

        var result = sb.ToString();

        if (!commandContext.ReplicaConnection)
        {
            commandContext.Socket.SendCommand(result);
        }

        return Task.FromResult(result);
    }
}