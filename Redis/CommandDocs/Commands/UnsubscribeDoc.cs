namespace Redis.CommandDocs.Commands;

public static class UnsubscribeDoc
{
    public static CommandDocumentation Instance { get; } = new(
        Name: "UNSUBSCRIBE",
        Summary: "Unsubscribes from a channel.",
        Syntax: "UNSUBSCRIBE [channel]",
        Group: "Pub/Sub",
        Complexity: "O(N) where N is the number of subscriptions to the channel."
    );
}
