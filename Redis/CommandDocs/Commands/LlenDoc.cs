namespace Redis.CommandDocs.Commands;

public static class LlenDoc
{
    public static CommandDocumentation Instance { get; } = new(
        Name: "LLEN",
        Summary: "Returns the length of a list.",
        Usage:
        [
            "LLEN mylist",
            "LLEN anotherlist"
        ]
    );
}
