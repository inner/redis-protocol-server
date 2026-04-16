namespace Redis.CommandDocs.Commands;

public static class DiscardDoc
{
    public static CommandDocumentation Instance { get; } = new(
        Name: "DISCARD",
        Summary: "Discards a transaction.",
        Usage:
        [
            "redis-cli",
            "MULTI",
            "SET mykey1 myval1",
            "INCR someotherkey",
            "DISCARD",
            "GET mykey1"
        ]
    );
}
