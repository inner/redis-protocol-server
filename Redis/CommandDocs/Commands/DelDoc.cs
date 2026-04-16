namespace Redis.CommandDocs.Commands;

public static class DelDoc
{
    public static CommandDocumentation Instance { get; } = new(
        Name: "DEL",
        Summary: "Deletes one or more keys.",
        Usage:
        [
            "redis-cli SET key1 val1",
            "redis-cli SET key2 val2",
            "redis-cli DEL key1 key2 key3"
        ]
    );
}
