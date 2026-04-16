namespace Redis.CommandDocs.Commands;

public static class LpopDoc
{
    public static CommandDocumentation Instance { get; } = new(
        Name: "LPOP",
        Summary: "Removes and returns the first element of the list stored at key.",
        Syntax: "LPOP key"
    );
}
