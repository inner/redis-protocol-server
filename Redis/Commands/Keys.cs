using System.Text;
using Redis.Cache;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Keys : Base
{
    protected override string Name => nameof(Keys);
    public override bool CanBePropagated => false;

    protected override Task<string> ExecuteCore(CommandContext commandContext)
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
        commandContext.Socket.SendCommand(result);

        return Task.FromResult(result);
    }
}
