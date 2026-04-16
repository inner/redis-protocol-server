namespace Redis.CommandDocs.Commands;

public static class ConfigDoc
{
    public static CommandDocumentation Instance { get; } = new(
        Name: "CONFIG",
        Summary: "A container for server configuration commands."
    );
}
