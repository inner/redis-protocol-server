namespace codecrafters_redis.RespCommands;

public class Get : Base
{
    public override string Execute(int commandCount, string[] commandParts)
    {
        var cacheKey = commandParts[4];
        var cacheItem = DataCache.Get(cacheKey);

        return cacheItem is null or { Value: null }
            ? Constants.NullResponse
            : $"${cacheItem.Value.Length}\r\n{cacheItem.Value}\r\n";
    }
}