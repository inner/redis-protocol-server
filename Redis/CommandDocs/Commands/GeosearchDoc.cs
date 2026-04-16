namespace Redis.CommandDocs.Commands;

public static class GeosearchDoc
{
    public static CommandDocumentation Instance { get; } = new(
        Name: "GEOSEARCH",
        Summary: "Query a sorted set representing a geospatial index to fetch members matching a given maximum distance from a point or within a bounding box.",
        Syntax: "GEOSEARCH key FROMMEMBER member BYRADIUS radius m|km|ft|mi [WITHCOORD] [WITHDIST] [WITHHASH] [COUNT count [ANY]] [ASC|DESC]",
        Group: "Geospatial",
        Complexity: "O(log(N)+M) with N being the number of elements in the sorted set and M the number of elements being returned."
    );
}
