namespace codecrafters_redis.RespCommands;

public abstract class RespCommandBase
{
    public abstract string Execute(int commandCount, string[] commandParts);
}