namespace Redis.CommandDocs.Commands;

public static class XrangeDoc
{
    public static CommandDocumentation Instance { get; } = new(
        Name: "XRANGE",
        Summary: "Returns the messages from a stream within a range of IDs.",
        Usage:
        [
            "redis-cli XADD weather_in_london 1-0 temperature 20 humidity 95",
            "redis-cli XADD weather_in_london 1-* temperature 19 humidity 70",
            "redis-cli XADD weather_in_london 2-* temperature 24 humidity 78",
            "redis-cli XADD weather_in_london 2-* temperature 24 humidity 78",
            "redis-cli XADD weather_in_london * temperature 25 humidity 90",
            "redis-cli XRANGE weather_in_london 1 1",
            "redis-cli XRANGE weather_in_london 1 1-1",
            "redis-cli XRANGE weather_in_london - 1",
            "redis-cli XRANGE weather_in_london - 1-0",
            "redis-cli XRANGE weather_in_london 2 +"
        ]
    );
}
