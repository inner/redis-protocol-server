namespace Redis.CommandDocs.Commands;

public static class SubscribeDoc
{
    public static CommandDocumentation Instance { get; } = new(
        Name: "SUBSCRIBE",
        Summary: "Subscribes to a channel to receive messages.",
        Syntax: "SUBSCRIBE channel",
        Since: "1.0.0",
        Group: "Pub/Sub",
        Complexity: "O(1) per message received."
    );
}
