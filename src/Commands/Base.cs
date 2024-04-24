namespace codecrafters_redis.RespCommands;

public abstract class Base
{
    public abstract string Execute(int commandCount, string[] commandParts);
}