namespace codecrafters_redis.RespCommands;

public class Get : RespCommandBase
{
    public override string Execute(int commandCount, string[] commandParts)
    {
        var cacheKey = commandParts[4];
        var cacheValue = DataCache.Get(cacheKey);

        return string.IsNullOrWhiteSpace(cacheValue)
            ? "$-1\r\n"
            : $"${cacheValue.Length}\r\n{cacheValue}\r\n";
    }
}