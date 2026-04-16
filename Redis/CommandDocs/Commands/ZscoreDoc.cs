namespace Redis.CommandDocs.Commands;

public static class ZscoreDoc
{
    public static CommandDocumentation Instance { get; } = new(
        Name: "ZSCORE",
        Summary: "Returns the score of a member in a sorted set.",
        Syntax: "ZSCORE key member",
        Group: "Sorted Sets",
        Complexity: "O(log(N)) where N is the number of elements in the sorted set."
    );
}
