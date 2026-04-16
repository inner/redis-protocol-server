namespace Redis.CommandDocs.Commands;

public static class EchoDoc
{
    public static CommandDocumentation Instance { get; } = new(
        Name: "ECHO",
        Summary: "Returns the given string.",
        Usage: ["redis-cli ECHO mystring"]
    );
}
