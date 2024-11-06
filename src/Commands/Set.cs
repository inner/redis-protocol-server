using System.Text;
using codecrafters_redis.Cache;
using codecrafters_redis.Common;

namespace codecrafters_redis.Commands;

public class Set : Base
{
    public override bool CanBePropagated => true;

    private const string Ok = "+OK";

    protected override Task<string> OnMasterNodeExecute(CommandContext commandContext)
    {
        var result = Constants.OkResponse;

        var cacheKey = commandContext.CommandDetails.CommandParts[4];
        var cacheValue = commandContext.CommandDetails.CommandParts[6];

        if (commandContext.CommandDetails.CommandParts.Length < 9)
        {
            DataCache.Set(cacheKey, cacheValue);

            if (!commandContext.CommandDetails.FromTransaction)
            {
                commandContext.Socket.Send(Encoding.UTF8.GetBytes(result));
            }

            return Task.FromResult(Ok);
        }

        const string expiryCommandConstant = "PX";

        var expiryCommand = commandContext.CommandDetails.CommandParts[8];
        if (!string.Equals(expiryCommand, expiryCommandConstant, StringComparison.InvariantCultureIgnoreCase))
        {
            throw new Exception($"Unrecognized command used for '{nameof(Set)}': '{expiryCommand}'.");
        }

        var expiry = int.Parse(commandContext.CommandDetails.CommandParts[10]);
        DataCache.Set(cacheKey, cacheValue, DateTimeOffset.Now.AddMilliseconds(expiry).ToUnixTimeMilliseconds());

        if (!commandContext.CommandDetails.FromTransaction)
        {
            commandContext.Socket.Send(Encoding.UTF8.GetBytes(result));
        }

        return Task.FromResult(Ok);
    }

    protected override Task<string> OnReplicaNodeExecute(CommandContext commandContext)
    {
        var cacheKey = commandContext.CommandDetails.CommandParts[4];
        var cacheValue = commandContext.CommandDetails.CommandParts[6];

        if (commandContext.CommandDetails.CommandParts.Length < 9)
        {
            DataCache.Set(cacheKey, cacheValue);
            return Task.FromResult(Ok);
        }

        const string expiryCommandConstant = "PX";

        var expiryCommand = commandContext.CommandDetails.CommandParts[8];
        if (!string.Equals(expiryCommand, expiryCommandConstant, StringComparison.InvariantCultureIgnoreCase))
        {
            throw new AggregateException($"Unrecognized command used for '{nameof(Set)}': '{expiryCommand}'.");
        }

        var expiry = int.Parse(commandContext.CommandDetails.CommandParts[10]);
        DataCache.Set(cacheKey, cacheValue, DateTimeOffset.Now.AddMilliseconds(expiry).ToUnixTimeMilliseconds());

        return Task.FromResult(Ok);
    }
}