namespace Redis.CommandDocs.Commands;

public static class ZcardDoc
{
    public static CommandDocumentation Instance { get; } = new(
        Name: "ZCARD",
        Summary: "Returns the number of members in a sorted set.",
        Syntax: "ZCARD key",
        Group: "Sorted Sets",
        Complexity: "O(1)"
    );
}
