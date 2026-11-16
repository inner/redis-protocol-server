using Redis.Cache;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

// PSETEX key milliseconds value: SET with a TTL in milliseconds. Clients such
// as StackExchange.Redis use it for timed string writes.
public class Psetex : Base
{
    protected override string Name => nameof(Psetex);
    public override bool CanBePropagated => true;

    protected override Task<string> ExecuteCore(CommandContext commandContext)
    {
        var parts = commandContext.CommandDetails.CommandParts;
        string resp;

        if (commandContext.CommandDetails.CommandCount != 4)
        {
            resp = RespBuilder.Error("wrong number of arguments for 'psetex' command");
            Reply(commandContext, resp);
            return Task.FromResult(resp);
        }

        if (!long.TryParse(parts[6], out var ttlMilliseconds) || ttlMilliseconds <= 0)
        {
            resp = RespBuilder.Error("invalid expire time in 'psetex' command");
            Reply(commandContext, resp);
            return Task.FromResult(resp);
        }

        DataCache.Set(
            parts[4],
            parts[8],
            DateTimeOffset.Now.AddMilliseconds(ttlMilliseconds).ToUnixTimeMilliseconds());

        resp = RespBuilder.SimpleString("OK");
        Reply(commandContext, resp);
        return Task.FromResult(resp);
    }
}
