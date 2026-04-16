namespace Redis.CommandDocs.Commands;

public static class XaddDoc
{
    public static CommandDocumentation Instance { get; } = new(
        Name: "XADD",
        Summary: "Appends a new entry to a stream.",
        Usage:
        [
            "redis-cli XADD weather_in_london 1-0 temperature 20 humidity 95",
            "redis-cli XADD weather_in_london 1-* temperature 19 humidity 70",
            "redis-cli XADD weather_in_london 2-* temperature 24 humidity 78",
            "redis-cli XADD weather_in_london 2-* temperature 24 humidity 78",
            "redis-cli XADD weather_in_london * temperature 25 humidity 90"
        ]
    );
}
