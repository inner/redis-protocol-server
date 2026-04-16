namespace Redis.CommandDocs.Commands;

public static class ExecDoc
{
    public static CommandDocumentation Instance { get; } = new(
        Name: "EXEC",
        Summary: "Executes all commands in a transaction.",
        Usage:
        [
            "redis-cli",
            "MULTI",
            "SET mykey1 myval1",
            "INCR someotherkey",
            "EXEC",
            "GET mykey1"
        ]
    );
}
