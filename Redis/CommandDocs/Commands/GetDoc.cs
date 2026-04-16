namespace Redis.CommandDocs.Commands;

public static class GetDoc
{
    public static CommandDocumentation Instance { get; } = new(
        Name: "GET",
        Summary: "Returns the string value of a key.",
        Usage: ["redis-cli GET key1"]
    );
}
