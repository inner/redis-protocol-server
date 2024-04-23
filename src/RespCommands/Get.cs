namespace codecrafters_redis.RespCommands;

public class Get : RespCommandBase
{
    public override string Execute(int commandCount, string[] commandParts)
    {
        var cacheKey = commandParts[4];
        var cacheItem = DataCache.Get(cacheKey);

        return cacheItem is null or { Value: null }
            ? "$-1\r\n"
            : $"${cacheItem.Value.Length}\r\n{cacheItem.Value}\r\n";
    }
}