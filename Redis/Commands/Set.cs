using Redis.Cache;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Set : Base
{
    public override bool CanBePropagated => true;
    public override Dictionary<string, Dictionary<string, string>> Docs()
    {
        return new()
        {
            {
                "SET",
                new()
                {
                    {
                        "summary",
                        "Sets the string value of a key, ignoring its type. The key is created if it doesn't exist."
                    },
                    { "usage #1", "redis-cli SET key1 val1" },
                    { "usage #2", "redis-cli SET key1 val1 PX 5000" }
                }
            }
        };
    }

    private readonly string okResp = RespBuilder.SimpleString("OK");

    protected override Task<string> OnMasterNodeExecute(CommandContext commandContext)
    {
        var cacheKey = commandContext.CommandDetails.CommandParts[4];
        var cacheValue = commandContext.CommandDetails.CommandParts[6];

        if (commandContext.CommandDetails.CommandParts.Length < 9)
        {
            DataCache.Set(cacheKey, cacheValue);

            if (!commandContext.CommandDetails.FromTransaction)
            {
                commandContext.Socket.SendCommand(okResp);
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

        if (!commandContext.CommandDetails.FromTransaction)
        {
            commandContext.Socket.SendCommand(okResp);
        }

        return Task.FromResult(okResp);
    }

    protected override Task<string> OnReplicaNodeExecute(CommandContext commandContext)
    {
        var cacheKey = commandContext.CommandDetails.CommandParts[4];
        var cacheValue = commandContext.CommandDetails.CommandParts[6];

        if (commandContext.CommandDetails.CommandParts.Length < 9)
        {
            DataCache.Set(cacheKey, cacheValue);
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

        return Task.FromResult(okResp);
    }
}