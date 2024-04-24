namespace codecrafters_redis.Commands;

public class Quit : Base
{
    public override string Execute(int commandCount, string[] commandParts)
    {
        return Constants.OkResponse;
    }
}