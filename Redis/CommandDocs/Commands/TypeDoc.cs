namespace Redis.CommandDocs.Commands;

public static class TypeDoc
{
    public static CommandDocumentation Instance { get; } = new(
        Name: "TYPE",
        Summary: "Determines the type of value stored at a key.",
        Usage:
        [
            "redis-cli SET mykey1 myval1",
            "redis-cli TYPE mykey1",
            "redis-cli TYPE nosuchkey",
            "redis-cli XADD stream1 * mykey1 myval1",
            "redis-cli TYPE stream1"
        ]
    );
}
