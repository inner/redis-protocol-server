namespace Redis.CommandDocs.Commands;

public static class GeoposDoc
{
    public static CommandDocumentation Instance { get; } = new(
        Name: "GEOPOS",
        Summary: "Returns the positions (longitude and latitude) of members of a geospatial index.",
        Syntax: "GEOPOS key member [member ...]",
        Group: "Geospatial",
        Complexity: "O(N) where N is the number of members to retrieve positions for."
    );
}
