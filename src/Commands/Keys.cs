using System.Text;
using codecrafters_redis.Cache;

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
        string result;
        var keys = DataCache.GetKeys(commandContext.CommandDetails.CommandParts[4]);
        if (keys.Count == 0)
        {
            result = "*0";
            commandContext.Socket.Send(Encoding.UTF8.GetBytes(result));
        }

        var sb = new StringBuilder($"*{keys.Count}\r\n");
        foreach (var key in keys)
        {
            sb.Append($"${key.Length}\r\n{key}\r\n");
        }

        result = sb.ToString();

        if (!commandContext.ReplicaConnection)
        {
            commandContext.Socket.Send(Encoding.UTF8.GetBytes(result));
        }

        return Task.FromResult(result);
    }
}