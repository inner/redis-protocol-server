using System.Text;
using Redis.Cache;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Keys : Base
{
    protected override string Name => nameof(Keys);
    public override bool CanBePropagated => false;

    public override Dictionary<string, Dictionary<string, string>> Docs()
    {
        return new()
        {
            {
                Name,
                new()
                {
                    {"summary", "Returns all key names that match a pattern."},
                    {"usage", "redis-cli KEYS *"}
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