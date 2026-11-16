using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Client : Base
{
    protected override string Name => nameof(Client);
    public override bool CanBePropagated => false;

    protected override Task<string> ExecuteCore(CommandContext commandContext)
    {
        var resp = RespBuilder.SimpleString("OK");
        Reply(commandContext, resp);

        return Task.FromResult(resp);
    }
}
