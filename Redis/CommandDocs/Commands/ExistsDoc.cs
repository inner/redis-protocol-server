namespace Redis.CommandDocs.Commands;

public static class ExistsDoc
{
    public static CommandDocumentation Instance { get; } = new(
        Name: "EXISTS",
        Summary: "Determines whether one or more keys exist.",
        Usage:
        [
            "redis-cli SET mykey1 myval1",
            "redis-cli SET mykey2 myval2",
            "redis-cli EXISTS mykey1 mykey2 nosuchkey"
        ]
    );
}
