using System.Text;

namespace codecrafters_redis.Rdb;

public static class RdbExtensions
{
    public static void ValidateRedisMagicString(this BinaryReader reader)
    {
        var magicStringBytes = reader.ReadBytes(RdbConstants.Verification.RedisMagicStringBytes);
        var magicString = Encoding.ASCII.GetString(magicStringBytes);
        
        if (magicString != RdbConstants.Verification.RedisMagicString)
        {
            throw new RdbReaderException("Invalid RDB file.");
        }
    }
    
    public static int GetRdbVersion(this BinaryReader reader)
    {
        var versionBytes = reader.ReadBytes(RdbConstants.Verification.RedisVersionBytes);
        var version = Encoding.ASCII.GetString(versionBytes);
        
        if (!int.TryParse(version, out var rdbVersion))
        {
            throw new RdbReaderException("Invalid RDB version.");
        }
        
        return rdbVersion;
    }
}