namespace Redis.CommandDocs.Commands;

public static class QuitDoc
{
    public static CommandDocumentation Instance { get; } = new(
        Name: "QUIT",
        Summary: "Closes the connection.",
        Usage: ["redis-cli QUIT"]
    );
}
