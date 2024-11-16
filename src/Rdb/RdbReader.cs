using codecrafters_redis.Cache;
using codecrafters_redis.Common;

namespace codecrafters_redis.Rdb;

public static class RdbReader
{
    public static void ReadRdb(string filePath)
    {
        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        using var binaryReader = new BinaryReader(fileStream);

        binaryReader.ValidateRedisMagicString();
        var version = binaryReader.GetRdbVersion();

        ulong? database = null;
        long expiry = 0;

        while (fileStream.Position < fileStream.Length)
        {
            var opCode = binaryReader.ReadByte();

            if (opCode == RdbConstants.OpCodes.SelectDb)
            {
                database = binaryReader.ReadLength();
                continue;
            }

            if (opCode == RdbConstants.OpCodes.ResizeDb)
            {
                var dbSize = binaryReader.ReadLength();
                var expireSize = binaryReader.ReadLength();
                Console.WriteLine($"DbSize: {dbSize} - {expireSize}");
                continue;
            }

            if (opCode == RdbConstants.OpCodes.Aux)
            {
                var auxKey = binaryReader.ReadStr();
                var auxKeyStr = auxKey.AsString();
                var auxVal = binaryReader.ReadStr();
                var auxValStr = auxVal.AsString();

                Console.WriteLine($"AuxField: {auxKeyStr} - {auxValStr}");
                continue;
            }

            if (opCode == RdbConstants.OpCodes.Eof)
            {
                if (version >= 5)
                {
                    binaryReader.ReadBytes(RdbConstants.Verification.Checksum);
                }

                break;
            }

            if (opCode == RdbConstants.OpCodes.ExpireTimeMs)
            {
                expiry = binaryReader.ReadInt64();
                opCode = binaryReader.ReadByte();
            }

            if (opCode == RdbConstants.OpCodes.ExpireTime)
            {
                // expiry = binaryReader.ReadInt32();
                // expireTime = binaryReader.ReadInt32().ToString();
                opCode = binaryReader.ReadByte();
            }

            if (!database.HasValue)
            {
                continue;
            }

            if (opCode == RdbConstants.DataTypes.String)
            {
                var keyBytes = binaryReader.ReadStr();
                var valueBytes = binaryReader.ReadStr();
                
                DataCache.Set(keyBytes.AsString(), valueBytes.AsString(), expiry);
            }
        }
    }
}