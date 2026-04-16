namespace Redis.CommandDocs.Commands;

public static class SetDoc
{
    public static CommandDocumentation Instance { get; } = new(
        Name: "SET",
        Summary: "Sets the string value of a key, ignoring its type. The key is created if it doesn't exist.",
        Usage:
        [
            "redis-cli SET key1 val1",
            "redis-cli SET key1 val1 PX 5000"
        ]
    );
}
