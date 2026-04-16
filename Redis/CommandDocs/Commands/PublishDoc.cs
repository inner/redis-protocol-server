namespace Redis.CommandDocs.Commands;

public static class PublishDoc
{
    public static CommandDocumentation Instance { get; } = new(
        Name: "PUBLISH",
        Summary: "Publishes a message to a channel.",
        Syntax: "PUBLISH channel message",
        Group: "Pub/Sub",
        Complexity: "O(N) where N is the number of subscribers to the channel."
    );
}
