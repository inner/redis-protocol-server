namespace Redis.Common;

public static class GeoCalculator
{
    public const string UnsupportedUnitError = "unsupported unit provided. please use M, KM, FT, MI";

    // Conversion factors to meters, as Redis defines them. Returns null for an
    // unknown unit.
    public static double? MetersPerUnit(string unit)
    {
        return unit.ToLowerInvariant() switch
        {
            "m" => 1,
            "km" => 1000,
            "mi" => 1609.34,
            "ft" => 0.3048,
            _ => null
        };
    }

    public static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double radius = 6372797.560856;
        var dLat = (lat2 - lat1) * Math.PI / 180.0;
        var dLon = (lon2 - lon1) * Math.PI / 180.0;

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        
        return radius * c;
    }
}