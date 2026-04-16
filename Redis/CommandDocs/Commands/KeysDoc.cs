namespace Redis.CommandDocs.Commands;

public static class KeysDoc
{
    public static CommandDocumentation Instance { get; } = new(
        Name: "KEYS",
        Summary: "Returns all key names that match a pattern.",
        Usage: ["redis-cli KEYS *"]
    );
}
