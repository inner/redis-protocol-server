namespace Redis.CommandDocs.Commands;

public static class ReplconfDoc
{
    public static CommandDocumentation Instance { get; } = new(
        Name: "REPLCONF",
        Summary: "An internal command for configuring the replication stream."
    );
}
