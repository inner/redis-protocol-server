namespace Redis.CommandDocs.Commands;

public static class GeodistDoc
{
    public static CommandDocumentation Instance { get; } = new(
        Name: "GEODIST",
        Summary: "Returns the distance between two members of a geospatial index.",
        Syntax: "GEODIST key member1 member2 [unit]",
        Group: "Geospatial",
        Complexity: "O(log(N)) where N is the number of elements in the sorted set."
    );
}
