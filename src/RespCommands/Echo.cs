namespace codecrafters_redis.RespCommands;

public class Echo : RespCommandBase
{
    public override string Execute(int commandCount, string[] commandParts)
    {
        return commandCount switch
        {
            2 => $"${commandParts[4].Length}\r\n{commandParts[4]}\r\n",
            _ => throw new ArgumentException("Wrong number of arguments for 'echo' command")
        };
    }
}