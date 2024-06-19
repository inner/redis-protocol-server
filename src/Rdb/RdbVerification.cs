using System.Text;

namespace codecrafters_redis.Rdb;

public static class RdbVerification
{
    public static void ValidateRedisMagicString(BinaryReader reader)
    {
        var magicStringBytes = reader.ReadBytes(RdbConstants.Verification.RedisMagicStringBytes);
        var magicString = Encoding.ASCII.GetString(magicStringBytes);
        if (magicString != RdbConstants.Verification.RedisMagicString)
        {
            throw new RdbReaderException("Invalid RDB file.");
        }
    }
    
    public static int ValidateRdbVersion(BinaryReader reader)
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