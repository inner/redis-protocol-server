namespace Redis.CommandDocs.Commands;

public static class XreadDoc
{
    public static CommandDocumentation Instance { get; } = new(
        Name: "XREAD",
        Summary: "Returns messages from multiple streams with IDs greater than the ones requested. Blocks until a message is available otherwise.",
        Usage: ["redis-cli XREAD block 10000 streams weather_in_london 2-0"]
    );
}
