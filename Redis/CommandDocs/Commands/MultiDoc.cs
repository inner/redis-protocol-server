namespace Redis.CommandDocs.Commands;

public static class MultiDoc
{
    public static CommandDocumentation Instance { get; } = new(
        Name: "MULTI",
        Summary: "Marks the start of a transaction block.",
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
