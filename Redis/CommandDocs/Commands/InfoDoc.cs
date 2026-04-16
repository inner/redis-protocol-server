namespace Redis.CommandDocs.Commands;

public static class InfoDoc
{
    public static CommandDocumentation Instance { get; } = new(
        Name: "INFO",
        Summary: "Returns information and statistics about the server.",
        Usage: ["redis-cli INFO"]
    );
}
