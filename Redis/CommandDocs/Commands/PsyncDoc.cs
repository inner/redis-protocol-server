namespace Redis.CommandDocs.Commands;

public static class PsyncDoc
{
    public static CommandDocumentation Instance { get; } = new(
        Name: "PSYNC",
        Summary: "An internal command used in replication."
    );
}
