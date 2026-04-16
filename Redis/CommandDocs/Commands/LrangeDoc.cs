namespace Redis.CommandDocs.Commands;

public static class LrangeDoc
{
    public static CommandDocumentation Instance { get; } = new(
        Name: "LRANGE",
        Summary: "Returns a range of elements from a list.",
        Usage:
        [
            "LRANGE mylist 0 -1",
            "LRANGE mylist 0 1",
            "LRANGE mylist 1 2"
        ]
    );
}
