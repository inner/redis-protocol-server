namespace codecrafters_redis;

public class ClientCommandExecutor
{
    public string Execute(string clientCommandString)
    {
        clientCommandString = clientCommandString
            .Replace("\n", "")
            .Trim();
        
        var commandParts = clientCommandString
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.ToLower())
            .ToArray();
        
        var commandType = commandParts[0].GetRespCommandType();
        
        return commandType switch
        {
            RespCommandType.Ping => "+PONG\\r\\n",
            RespCommandType.Echo => $"${commandParts[1].Length}\\r\\n{commandParts[1]}\\r\\n",
            _ => throw new ArgumentException("Invalid client command.")
        };
    }
}