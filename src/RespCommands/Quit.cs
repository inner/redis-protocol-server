namespace codecrafters_redis.RespCommands;

public class Quit : RespCommandBase
{
    public override string Execute(int commandCount, string[] commandParts)
    {
        return "+OK\r\n";
    }
}