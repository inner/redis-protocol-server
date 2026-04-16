namespace Redis.CommandDocs.Commands;

public static class ClientDoc
{
    public static CommandDocumentation Instance { get; } = new(
        Name: "CLIENT",
        Summary: "A container for client connection commands."
    );
}
