namespace Redis.CommandDocs.Commands;

public static class ZremDoc
{
    public static CommandDocumentation Instance { get; } = new(
        Name: "ZREM",
        Summary: "Removes one or more members from a sorted set.",
        Syntax: "ZREM key member [member ...]",
        Group: "Sorted Sets",
        Complexity: "O(log(N)) for each item removed, where N is the number of elements in the sorted set."
    );
}
