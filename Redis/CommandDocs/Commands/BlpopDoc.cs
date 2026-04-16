namespace Redis.CommandDocs.Commands;

public static class BlpopDoc
{
    public static CommandDocumentation Instance { get; } = new(
        Name: "BLPOP",
        Summary: "Removes and returns the first element of the list stored at key. If the list is empty, it blocks until an element is available or the timeout is reached.",
        Syntax: "BLPOP key [key ...] timeout",
        Since: "1.0.0",
        Group: "List",
        Complexity: "O(1) for each key"
    );
}
