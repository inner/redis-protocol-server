namespace Redis.CommandDocs.Commands;

public static class IncrDoc
{
    public static CommandDocumentation Instance { get; } = new(
        Name: "INCR",
        Summary: "Increments the integer value of a key by one. Uses 0 as initial value if the key doesn't exist.",
        Usage: ["redis-cli INCR mykey1"]
    );
}
