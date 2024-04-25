namespace codecrafters_redis.Commands;

public class Psync : Base
{
    public override string Execute(int commandCount, string[] commandParts)
    {
        return $"+FULLRESYNC {ServerInfo.MasterReplId} 0\r\n";
    }
}