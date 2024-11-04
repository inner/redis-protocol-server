using System.Text;

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

        if (length == RdbConstants.EncType.Int8)
        {
            var tmp = binaryReader.ReadBytes(RdbConstants.One);
            return Encoding.UTF8.GetBytes(((sbyte)tmp[0]).ToString());
        }

        if (length == RdbConstants.EncType.Int16)
        {
            var tmp = binaryReader.ReadBytes(RdbConstants.Two);
            return Encoding.UTF8.GetBytes(BitConverter.ToInt16(tmp).ToString());
        }

        if (length == RdbConstants.EncType.Int32)
        {
            var tmp = binaryReader.ReadBytes(RdbConstants.Four);
            return Encoding.UTF8.GetBytes(BitConverter.ToInt32(tmp).ToString());
        }

        if (length == RdbConstants.EncType.Lzf)
        {
            var clen = ReadLength(binaryReader);
            var ulen = ReadLength(binaryReader);

            var compressed = binaryReader.ReadBytes((int)clen);
            var decompressed = LzfDecompress(compressed, (int)ulen);

            if (decompressed.Length != (int)ulen)
                throw new RdbReaderException(
                    $"decompressed string length {decompressed.Length} didn't match expected length {(int)ulen}");

            return decompressed;
        }

        throw new RdbReaderException($"Invalid string encoding {length}");
    }

    private static (ulong Length, bool IsEncoded) ReadLengthWithEncoding(this BinaryReader binaryReader)
    {
        // https://github.com/sripathikrishnan/redis-rdb-tools/wiki/Redis-RDB-Dump-File-Format#length-encoding
        ulong len;
        var isEncoded = false;

        var b = binaryReader.ReadByte();

        // 8bit right shift 6bit, get the starting 2bit
        // 0xC0  11000000
        // 0x3F  00111111
        var encType = (b & 0xC0) >> 6;

        if (encType == RdbConstants.LengthEncoding.Bit6)
        {
            // starting bits are 00
            len = (ulong)(b & 0x3F);
        }
        else if (encType == RdbConstants.LengthEncoding.Bit14)
        {
            // starting bits are 01
            var b1 = binaryReader.ReadByte();
            len = (ulong)((b & 0x3F) << 8 | b1);
        }
        else if (encType == RdbConstants.LengthEncoding.EncVal)
        {
            // starting bits are 11
            len = (ulong)(b & 0x3F);
            isEncoded = true;
        }
        else if (b == RdbConstants.LengthEncoding.Bit32)
        {
            // starting bits are 10
            len = binaryReader.ReadUInt32BigEndian();
        }
        else if (b == RdbConstants.LengthEncoding.Bit64)
        {
            len = binaryReader.ReadUInt32BigEndian();
        }
        else
        {
            throw new RdbReaderException($"Invalid string encoding {encType} (encoding byte {b})");
        }

        return (len, isEncoded);
    }

    private static byte[] LzfDecompress(byte[] compressed, int ulen)
    {
        var outStream = new List<byte>(ulen);
        var outIndex = 0;

        var inLen = compressed.Length;
        var inIndex = 0;

        while (inIndex < inLen)
        {
            var ctrl = compressed[inIndex];

            inIndex += 1;

            if (ctrl < 32)
            {
                for (int i = 0; i < ctrl + 1; i++)
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