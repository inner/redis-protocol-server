namespace codecrafters_redis.RespCommands;

public class Quit : CommandBase
{
    public override string Execute(int commandCount, string[] commandParts)
    {
        return Constants.OkResponse;
    }
}