namespace Redis.Common;

using System;

public class GeohashEncoder
{
    private const double MinLatitude = -85.05112878;
    private const double MaxLatitude = 85.05112878;
    private const double MinLongitude = -180;
    private const double MaxLongitude = 180;
    private const double LatitudeRange = MaxLatitude - MinLatitude;
    private const double LongitudeRange = MaxLongitude - MinLongitude;

    public static long Encode(double latitude, double longitude)
    {
        var normalizedLatitude = Math.Pow(2, 26) * (latitude - MinLatitude) / LatitudeRange;
        var normalizedLongitude = Math.Pow(2, 26) * (longitude - MinLongitude) / LongitudeRange;
        
        var normalizedLatitudeInt = (int)normalizedLatitude;
        var normalizedLongitudeInt = (int)normalizedLongitude;

        return Interleave(normalizedLatitudeInt, normalizedLongitudeInt);
    }

    private static long Interleave(int x, int y)
    {
        var spreadX = SpreadInt32ToInt64(x);
        var spreadY = SpreadInt32ToInt64(y);
        var yShifted = spreadY << 1;
        return spreadX | yShifted;
    }

    private static long SpreadInt32ToInt64(int v)
    {
        var result = v & 0xFFFFFFFF;
        result = (result | (result << 16)) & 0x0000FFFF0000FFFF;
        result = (result | (result << 8)) & 0x00FF00FF00FF00FF;
        result = (result | (result << 4)) & 0x0F0F0F0F0F0F0F0F;
        result = (result | (result << 2)) & 0x3333333333333333;
        result = (result | (result << 1)) & 0x5555555555555555;
        return result;
    }
}