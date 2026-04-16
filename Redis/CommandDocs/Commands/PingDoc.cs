namespace Redis.CommandDocs.Commands;

public static class PingDoc
{
    public static CommandDocumentation Instance { get; } = new(
        Name: "PING",
        Summary: "Returns the server's liveliness response.",
        Usage: ["redis-cli PING"]
    );
}
