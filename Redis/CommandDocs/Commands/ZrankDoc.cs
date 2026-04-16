namespace Redis.CommandDocs.Commands;

public static class ZrankDoc
{
    public static CommandDocumentation Instance { get; } = new(
        Name: "ZRANK",
        Summary: "Returns the rank (or index) of a member in a sorted set.",
        Syntax: "ZRANK key member",
        Group: "Sorted Sets",
        Complexity: "O(log(N)) where N is the number of elements in the sorted set."
    );
}
