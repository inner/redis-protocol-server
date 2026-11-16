using Redis.Cache;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Set : Base
{
    protected override string Name => nameof(Set);
    public override bool CanBePropagated => true;
    private static readonly string OkResp = RespBuilder.SimpleString("OK");

    // SET key value [EX seconds | PX milliseconds]
    // TODO: NX/XX, GET, KEEPTTL, EXAT and PXAT are not supported.
    protected override Task<string> ExecuteCore(CommandContext commandContext)
    {
        var parts = commandContext.CommandDetails.CommandParts;
        var key = parts[4];
        var value = parts[6];
        long? ttlMilliseconds = null;
        string resp;

        // Options start at the 4th argument; each takes one value.
        for (var i = 8; i < parts.Length; i += 4)
        {
            var option = parts[i].ToUpperInvariant();

            if (option is not ("EX" or "PX") || ttlMilliseconds != null)
            {
                resp = RespBuilder.Error("syntax error");
                Reply(commandContext, resp);
                return Task.FromResult(resp);
            }

            if (i + 2 >= parts.Length || !long.TryParse(parts[i + 2], out var amount) || amount <= 0)
            {
                resp = RespBuilder.Error("invalid expire time in 'set' command");
                Reply(commandContext, resp);
                return Task.FromResult(resp);
            }

            ttlMilliseconds = option == "EX" ? amount * 1000 : amount;
        }

        DataCache.Set(
            key,
            value,
            ttlMilliseconds == null
                ? null
                : DateTimeOffset.Now.AddMilliseconds(ttlMilliseconds.Value).ToUnixTimeMilliseconds());

        Reply(commandContext, OkResp);
        return Task.FromResult(OkResp);
    }
}
