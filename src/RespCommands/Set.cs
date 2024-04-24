namespace codecrafters_redis.RespCommands;

public class Set : CommandBase
{
    public override string Execute(int commandCount, string[] commandParts)
    {
        var cacheKey = commandParts[4];
        var cacheValue = commandParts[6];
        
        if (commandParts.Length < 9)
        {
            DataCache.Set(cacheKey, cacheValue);
            return Constants.OkResponse;
        }
        
        const string expiryCommandConstant = "PX";

        var expiryCommand = commandParts[8];
        if (!string.Equals(expiryCommand, expiryCommandConstant, StringComparison.InvariantCultureIgnoreCase))
        {
            throw new AggregateException($"Unrecognized command used for '{nameof(Set)}': '{expiryCommand}'.");
        }
        
        var expiry = int.Parse(commandParts[10]);
        DataCache.Set(cacheKey, cacheValue, expiry);

        return Constants.OkResponse;
    }
}