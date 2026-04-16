namespace Redis.CommandDocs.Commands;

public static class ZaddDoc
{
    public static CommandDocumentation Instance { get; } = new(
        Name: "ZADD",
        Summary: "Adds one or more members to a sorted set, or updates the score of an existing member.",
        Syntax: "ZADD key [NX|XX] [CH] [INCR] score member [score member ...]",
        Group: "Sorted Sets",
        Complexity: "O(log(N)) for each item added, where N is the number of elements in the sorted set."
    );
}
