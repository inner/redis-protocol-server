namespace Redis.CommandDocs.Commands;

public static class GeoaddDoc
{
    public static CommandDocumentation Instance { get; } = new(
        Name: "GEOADD",
        Summary: "Adds one or more geospatial items (latitude, longitude, name) to a geospatial index represented using a sorted set.",
        Syntax: "GEOADD key longitude latitude member [longitude latitude member ...]",
        Group: "Geospatial",
        Complexity: "O(log(N)) for each item added, where N is the number of elements in the sorted set."
    );
}
