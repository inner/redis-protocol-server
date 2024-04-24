namespace codecrafters_redis.RespCommands;

public abstract class CommandBase
{
    public abstract string Execute(int commandCount, string[] commandParts);
}