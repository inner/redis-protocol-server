namespace Redis.CommandDocs.Commands;

public static class ZrangeDoc
{
    public static CommandDocumentation Instance { get; } = new(
        Name: "ZRANGE",
        Summary: "Returns a range of members in a sorted set, by index.",
        Syntax: "ZRANGE key start stop",
        Group: "Sorted Sets",
        Complexity: "O(log(N) + M) where N is the number of elements in the sorted set and M is the number of elements returned."
    );
}
