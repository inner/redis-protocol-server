using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Watch : Base
{
    protected override string Name => nameof(Watch);
    public override bool CanBePropagated => false;
    protected override Task<string> ExecuteCore(CommandContext commandContext)
    {
        var response = RespBuilder.SimpleString("OK");
        SendIfNotFromTransaction(commandContext, response);
        return Task.FromResult(response);
    }
}