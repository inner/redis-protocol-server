namespace Redis.CommandDocs.Commands;

public static class WaitDoc
{
    public static CommandDocumentation Instance { get; } = new(
        Name: "WAIT",
        Summary: "Blocks until the asynchronous replication of all preceding write commands sent by the connection is completed.",
        Documentation: "https://redis.io/docs/latest/commands/wait/",
        Usage:
        [
            "redis-cli",
            "SET mykey1 myval1",
            "SET mykey2 myval2",
            "SET mykey3 myval3",
            "WAIT 4 2000"
        ]
    );
}
