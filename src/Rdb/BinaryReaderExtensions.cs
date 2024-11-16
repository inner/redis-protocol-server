using codecrafters_redis.Common;

namespace codecrafters_redis.Rdb;

public static class BinaryReaderExtensions
{
    public static ulong ReadLength(this BinaryReader br)
    {
        var (len, _) = ReadLengthWithEncoding(br);
        return len;
    }

    public static byte[] ReadStr(this BinaryReader binaryReader)
    {
        // https://github.com/sripathikrishnan/redis-rdb-tools/wiki/Redis-RDB-Dump-File-Format#string-encoding
        var (length, isEncoded) = ReadLengthWithEncoding(binaryReader);

        if (!isEncoded)
        {
            return binaryReader.ReadBytes((int)length);
        }

        switch (length)
        {
            case RdbConstants.EncType.Int8:
            {
                var tmp = binaryReader.ReadBytes(RdbConstants.One);
                return ((sbyte)tmp[0]).ToString().AsBytes();
            }
            case RdbConstants.EncType.Int16:
            {
                var tmp = binaryReader.ReadBytes(RdbConstants.Two);
                return BitConverter.ToInt16(tmp).ToString().AsBytes();
            }
            case RdbConstants.EncType.Int32:
            {
                var tmp = binaryReader.ReadBytes(RdbConstants.Four);
                return BitConverter.ToInt32(tmp).ToString().AsBytes();
            }
            case RdbConstants.EncType.Lzf:
            {
                var compressedLen = ReadLength(binaryReader);
                var uncompressedLen = ReadLength(binaryReader);

                var compressed = binaryReader.ReadBytes((int)compressedLen);
                var decompressed = LzfDecompress(compressed, (int)uncompressedLen);

                if (decompressed.Length != (int)uncompressedLen)
                    throw new RdbReaderException(
                        $"decompressed string length {decompressed.Length} didn't match expected length {(int)uncompressedLen}");

                return decompressed;
            }
            default:
                throw new RdbReaderException($"Invalid string encoding {length}");
        }
    }

    private static (ulong Length, bool IsEncoded) ReadLengthWithEncoding(this BinaryReader binaryReader)
    {
        // https://github.com/sripathikrishnan/redis-rdb-tools/wiki/Redis-RDB-Dump-File-Format#length-encoding
        ulong len;
        var isEncoded = false;

        var @byte = binaryReader.ReadByte();
        
        // 8bit right shift 6bit, get the starting 2bit
        // 0xC0  11000000
        // 0x3F  00111111
        var encodingType = (@byte & 0xC0) >> 6;

        switch (encodingType)
        {
            case RdbConstants.LengthEncoding.Bit6:
                // starting bits are 00
                len = (ulong)(@byte & 0x3F);
                break;
            case RdbConstants.LengthEncoding.Bit14:
            {
                // starting bits are 01
                var b1 = binaryReader.ReadByte();
                len = (ulong)((@byte & 0x3F) << 8 | b1);
                break;
            }
            case RdbConstants.LengthEncoding.EncVal:
                // starting bits are 11
                len = (ulong)(@byte & 0x3F);
                isEncoded = true;
                break;
            default:
            {
                len = @byte switch
                {
                    RdbConstants.LengthEncoding.Bit32 or RdbConstants.LengthEncoding.Bit64 =>
                        // starting bits are 10
                        binaryReader.ReadUInt32BigEndian(),
                    _ => throw new RdbReaderException($"Invalid string encoding {encodingType} (encoding byte {@byte})")
                };

                break;
            }
        }

        return (len, isEncoded);
    }

    private static byte[] LzfDecompress(byte[] compressed, int uncompressedLength)
    {
        var outStream = new List<byte>(uncompressedLength);
        var outIndex = 0;

        var inLen = compressed.Length;
        var inIndex = 0;

        while (inIndex < inLen)
        {
            var ctrl = compressed[inIndex];

            inIndex += 1;

            if (ctrl < 32)
            {
                for (var i = 0; i < ctrl + 1; i++)
                {
                    outStream.Add(compressed[inIndex]);
                    inIndex += 1;
                    outIndex += 1;
                }
            }
            else
            {
                var length = ctrl >> 5;
                if (length == 7)
                {
                    length += compressed[inIndex];
                    inIndex += 1;
                }

                var @ref = outIndex - ((ctrl & 0x1f) << 8) - compressed[inIndex] - 1;
                inIndex += 1;

                for (var i = 0; i < length + 2; i++)
                {
                    outStream.Add(outStream[@ref]);
                    @ref += 1;
                    outIndex += 1;
                }
            }
        }

        return outStream.ToArray();
    }

    private static uint ReadUInt32BigEndian(this BinaryReader br)
    {
        var bytes = br.ReadBytes(RdbConstants.Four);
        Array.Reverse(bytes);
        return BitConverter.ToUInt32(bytes, 0);
    }
}