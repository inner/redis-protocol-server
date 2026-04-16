namespace Redis.CommandDocs.Commands;

public static class LpushDoc
{
    public static CommandDocumentation Instance { get; } = new(
        Name: "LPUSH",
        Summary: "Prepends one or multiple values to the beginning of a list.",
        Usage:
        [
            "LPUSH mylist value1 value2",
            "LPUSH anotherlist value3"
        ]
    );
}
