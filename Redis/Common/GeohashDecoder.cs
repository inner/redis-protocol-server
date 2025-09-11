namespace Redis.Common;

using System;

public class GeohashDecoder
{
    public const double MinLatitude = -85.05112878;
    public const double MaxLatitude = 85.05112878;
    public const double MinLongitude = -180;
    public const double MaxLongitude = 180;
    private const double LatitudeRange = MaxLatitude - MinLatitude;
    private const double LongitudeRange = MaxLongitude - MinLongitude;

    public static (double latitude, double longitude) Decode(long geoCode)
    {
        var y = geoCode >> 1;
        var x = geoCode;
        
        var gridLatitudeNumber = CompactInt64ToInt32(x);
        var gridLongitudeNumber = CompactInt64ToInt32(y);

        return ConvertGridNumbersToCoordinates(gridLatitudeNumber, gridLongitudeNumber);
    }

    private static int CompactInt64ToInt32(long v)
    {
        v = v & 0x5555555555555555;
        v = (v | (v >> 1)) & 0x3333333333333333;
        v = (v | (v >> 2)) & 0x0F0F0F0F0F0F0F0F;
        v = (v | (v >> 4)) & 0x00FF00FF00FF00FF;
        v = (v | (v >> 8)) & 0x0000FFFF0000FFFF;
        v = (v | (v >> 16)) & 0x00000000FFFFFFFF;
        return (int)v;
    }

    private static (double latitude, double longitude) ConvertGridNumbersToCoordinates(int gridLatitudeNumber,
        int gridLongitudeNumber)
    {
        var gridLatitudeMin = MinLatitude + LatitudeRange * (gridLatitudeNumber / Math.Pow(2, 26));
        var gridLatitudeMax = MinLatitude + LatitudeRange * ((gridLatitudeNumber + 1) / Math.Pow(2, 26));
        var gridLongitudeMin = MinLongitude + LongitudeRange * (gridLongitudeNumber / Math.Pow(2, 26));
        var gridLongitudeMax = MinLongitude + LongitudeRange * ((gridLongitudeNumber + 1) / Math.Pow(2, 26));
        
        var latitude = (gridLatitudeMin + gridLatitudeMax) / 2;
        var longitude = (gridLongitudeMin + gridLongitudeMax) / 2;

        return (latitude, longitude);
    }
}