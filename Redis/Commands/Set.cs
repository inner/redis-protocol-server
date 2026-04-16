using Redis.Cache;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Set : Base
{
    protected override string Name => nameof(Set);
    public override bool CanBePropagated => true;
    private readonly string okResp = RespBuilder.SimpleString("OK");

    protected override Task<string> ExecuteCore(CommandContext commandContext)
    {
        var cacheKey = commandContext.CommandDetails.CommandParts[4];
        var cacheValue = commandContext.CommandDetails.CommandParts[6];
        var shouldSendOkResponse =
            ServerInfo.ServerRuntimeContext.IsMaster &&
            !commandContext.CommandDetails.FromTransaction;

        if (commandContext.CommandDetails.CommandParts.Length < 9)
        {
            DataCache.Set(cacheKey, cacheValue);

            if (shouldSendOkResponse)
            {
                SendIfNotFromTransaction(commandContext, okResp);
            }

            return Task.FromResult(okResp);
        }

        const string expiryCommandConstant = "PX";

        var expiryCommand = commandContext.CommandDetails.CommandParts[8];
        if (!string.Equals(expiryCommand, expiryCommandConstant, StringComparison.InvariantCultureIgnoreCase))
        {
            commandContext.Socket.SendCommand(RespBuilder.Error($"Unrecognized command: '{expiryCommand}'."));
        }

        var expiry = int.Parse(commandContext.CommandDetails.CommandParts[10]);
        DataCache.Set(cacheKey, cacheValue, DateTimeOffset.Now.AddMilliseconds(expiry).ToUnixTimeMilliseconds());

        if (shouldSendOkResponse)
        {
            SendIfNotFromTransaction(commandContext, okResp);
        }

        return Task.FromResult(okResp);
    }
}
