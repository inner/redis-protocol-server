namespace codecrafters_redis.Commands;

public abstract class Base
{
    public abstract string Execute(int commandCount, string[] commandParts);
}