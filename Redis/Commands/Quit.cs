using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Quit : Base
{
    protected override string Name => nameof(Quit);
    public override bool CanBePropagated => false;

    protected override Task<string> ExecuteCore(CommandContext commandContext)
    {
        var resp = RespBuilder.SimpleString("OK");
        Reply(commandContext, resp);

        return Task.FromResult(resp);
    }
}
