namespace Redis.CommandDocs.Commands;

public static class AuthDoc
{
    public static CommandDocumentation Instance { get; } = new(
        Name: "AUTH",
        Summary: "Authenticate to the server",
        Since: "1.0.0",
        Group: "connection",
        Complexity: "1",
        Arity: "2..N",
        ContainerCommands: false,
        History: "AUTH was added in Redis 1.0.0.",
        Notes: "The AUTH command is used to authenticate a client connection to the Redis server using a password."
    );
}
