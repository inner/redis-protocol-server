namespace codecrafters_redis.RespCommands;

public class Set : RespCommandBase
{
    public override string Execute(int commandCount, string[] commandParts)
    {
        var cacheKey = commandParts[4];
        var cacheValue = commandParts[6];
        
        DataCache.Add(cacheKey, cacheValue);
        
        return "+OK\r\n";
    }
}