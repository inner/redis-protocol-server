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
        
        var commandType = GetClientCommandType(commandParts[0]);
        
        return commandType switch
        {
            ClientCommandType.Ping => "+PONG\\r\\n",
            ClientCommandType.Echo => $"${commandParts[1].Length}\\r\\n{commandParts[1]}\\r\\n",
            _ => throw new ArgumentException("Invalid client command.")
        };
    }

    private ClientCommandType GetClientCommandType(string clientCommand)
    {
        if (string.IsNullOrWhiteSpace(clientCommand))
        {
            throw new ArgumentException("Invalid client command - empty or whitespace.");
        }

        if (string.Equals(clientCommand, ClientCommandType.Ping.ToString(),
                StringComparison.InvariantCultureIgnoreCase))
        {
            return ClientCommandType.Ping;
        }

        if (string.Equals(clientCommand, ClientCommandType.Echo.ToString(),
                StringComparison.InvariantCultureIgnoreCase))
        {
            return ClientCommandType.Echo;
        }

        throw new ArgumentException($"Invalid client command: {clientCommand}");
    }
}

public enum ClientCommandType
{
    Ping,
    Echo
}