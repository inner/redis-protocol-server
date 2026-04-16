namespace Redis.CommandDocs.Commands;

public static class RpushDoc
{
    public static CommandDocumentation Instance { get; } = new(
        Name: "RPUSH",
        Summary: "Appends one or multiple values to the end of a list.",
        Usage:
        [
            "RPUSH mylist value1 value2",
            "RPUSH anotherlist value3"
        ]
    );
}
