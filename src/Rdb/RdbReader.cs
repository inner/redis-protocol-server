using System.Text;

namespace codecrafters_redis.Rdb;

public class RdbReader
{
    private const string RedisMagicString = "REDIS";

    public List<string> ReadRdb(string filePath)
    {
        var keys = new List<string>();

        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        using var reader = new BinaryReader(fs);
        ReadHeader(reader);

        while (fs.Position < fs.Length)
        {
            var opCode = reader.ReadByte();

            if (opCode != (byte)RdbOpCode.SelectDb)
            {
                continue;
            }
            
            if (opCode == (byte)RdbOpCode.SelectDb)
            {
                Console.WriteLine($"SelectDb (0xFE) found at position {fs.Position - 1}");
            
                var dbNumber = ReadLength(reader);
                Console.WriteLine($"DB Number: {dbNumber}");
                
                var resizedbField = reader.ReadByte();
                var resizedbFieldHex = resizedbField.ToString("X2");
                
                var databaseHashTableSize = reader.ReadByte();
                var databaseHashTableSizeHex = databaseHashTableSize.ToString("X2");
            
                var expiryHashTableSize = ReadLength(reader);
                var expiryHashTableSizeHex = expiryHashTableSize.ToString("X2");
                
                var val2 = reader.ReadByte();
                var val2Hex = val2.ToString("X2");
                var val3 = reader.ReadByte();
                var val3Hex = val3.ToString("X2");

                var key1 = reader.ReadBytes(val3);
                var keyStr = Encoding.UTF8.GetString(key1);
                
                var val4 = reader.ReadByte();
                var val4Hex = val4.ToString("X2");
                var val5 = reader.ReadByte();
                var val5Hex = val5.ToString("X2");
                var val6 = reader.ReadByte();
                var val6Hex = val6.ToString("X2");
                var val7 = reader.ReadByte();
                var val7Hex = val7.ToString("X2");
                var val8 = reader.ReadByte();
                var val8Hex = val8.ToString("X2");
                var val9 = reader.ReadByte();
                var val9Hex = val9.ToString("X2");
                var val10 = reader.ReadByte();
                var val10Hex = val10.ToString("X2");
            }
            
            // var val2 = reader.ReadByte();
            // var val2Hex = val2.ToString("X2");
            // var val3 = reader.ReadByte();
            // var val3Hex = val3.ToString("X2");
            // var val4 = reader.ReadByte();
            // var val4Hex = val4.ToString("X2");
            // var val5 = reader.ReadByte();
            // var val5Hex = val5.ToString("X2");
            // var val6 = reader.ReadByte();
            // var val6Hex = val6.ToString("X2");
            // var val7 = reader.ReadByte();
            // var val7Hex = val7.ToString("X2");
            // var val8 = reader.ReadByte();
            // var val8Hex = val8.ToString("X2");
            // var val9 = reader.ReadByte();
            // var val9Hex = val9.ToString("X2");
            // var val10 = reader.ReadByte();
            // var val10Hex = val10.ToString("X2");
            
            // Key Expiry Timestamp. This is optional.
            // 1 byte flag indicating the value type.
            // The key, encoded as a Redis String. See String Encoding.
            // The value, encoded according to the value type. See Value Encoding.
            // The key expiry timestamp is optional. If present, it is a 4 byte unsigned integer in little endian byte order.
            // The value type flag is a single byte. The possible values are:
            // 0x00 - String
            // 0x01 - List
            // 0x02 - Set
            // 0x03 - Sorted Set
            // 0x04 - Hash
            // 0x09 - ZipMap
            // 0x0a - ZipList
            // 0x0b - IntSet
            // 0x0c - QuickList
            // 0x0d - Module
            // 0x0e - Module 2
            // 0x0f - Stream
            
            // var val2 = reader.ReadByte();
            // var val2Hex = val2.ToString("X2");
            // var val3 = reader.ReadByte();
            // var val3Hex = val3.ToString("X2");
            // var val4 = reader.ReadByte();
            // var val4Hex = val4.ToString("X2");
            // var val5 = reader.ReadByte();
            // var val5Hex = val5.ToString("X2");
            // var val6 = reader.ReadByte();
            // var val6Hex = val6.ToString("X2");
            // var val7 = reader.ReadByte();
            // var val7Hex = val7.ToString("X2");
            // var val8 = reader.ReadByte();
            // var val8Hex = val8.ToString("X2");
            // var val9 = reader.ReadByte();
            // var val9Hex = val9.ToString("X2");
            // var val10 = reader.ReadByte();
            // var val10Hex = val10.ToString("X2");

            // print each case with if
            // if (opCode == (byte)RdbOpCode.Aux)
            // {
            //     Console.WriteLine($"Aux Field (0xFA) found at position {fs.Position - 1}");
            //
            //     var auxKey = ReadString(reader);
            //     var auxValue = ReadString(reader, auxKey);
            //     
            //     if (auxKey == "redis-ver" || auxKey == "redis-bits" || auxKey == "ctime" || auxKey == "used-mem" ||
            //         auxKey == "aof-preamble")
            //     {
            //         Console.WriteLine($"{auxKey}: {auxValue}");
            //     }
            // }
            // else if (opCode == (byte)RdbOpCode.SelectDb)
            // {
            //     Console.WriteLine($"SelectDb (0xFE) found at position {fs.Position - 1}");
            //
            //     var dbNumber = ReadLength(reader);
            //     Console.WriteLine($"DB Number: {dbNumber}");
            // }
            // else if (opCode == (byte)RdbOpCode.ExpireTime || opCode == (byte)RdbOpCode.ExpireTimeMs)
            // {
            //     Console.WriteLine($"ExpireTime (0x{opCode:X2}) found at position {fs.Position - 1}");
            //
            //     reader.ReadBytes(8); // Skip 8 bytes of expire time
            // }
            // else if (opCode == (byte)RdbOpCode.Eof)
            // {
            //     Console.WriteLine($"EOF (0xFF) found at position {fs.Position - 1}");
            //     break;
            // }
            // else
            // {
            //     Console.WriteLine($"Unhandled OpCode (0x{opCode:X2}) found at position {fs.Position - 1}");
            // }
        }

        return keys;
    }

    private static void ReadHeader(BinaryReader reader)
    {
        var magic = Encoding.ASCII.GetString(reader.ReadBytes(5));
        if (magic != RedisMagicString)
        {
            throw new InvalidDataException("Invalid RDB file.");
        }

        var version = Encoding.ASCII.GetString(reader.ReadBytes(4));
        if (!int.TryParse(version, out _))
        {
            throw new InvalidDataException("Invalid RDB version.");
        }
    }

    private static string ReadString(BinaryReader? reader, string? key = null)
    {
        if (key != null && key.Contains("redis-bits"))
        {
        }

        int length = ReadLength(reader);
        byte[] bytes = reader.ReadBytes(length);
        return Encoding.UTF8.GetString(bytes);
    }

    private static int ReadLength(BinaryReader stream)
    {
        int length = 0;
        
        // Read the first byte from the stream
        int firstByte = stream.ReadByte();

        // Get the two most significant bits
        int mostSignificantBits = firstByte >> 6;

        switch (mostSignificantBits)
        {
            // 00: The next 6 bits represent the length
            case 0b00:
                // Mask to get the last 6 bits
                length = firstByte & 0b00111111;
                break;

            // 01: Read one additional byte. The combined 14 bits represent the length
            case 0b01:
                int secondByte = stream.ReadByte();
                length = ((firstByte & 0b00111111) << 8) | secondByte;
                break;

            // 10: Discard the remaining 6 bits. The next 4 bytes from the stream represent the length
            case 0b10:
                length = 0;
                for (int i = 0; i < 4; i++)
                {
                    int nextByte = stream.ReadByte();
                    length = (length << 8) | nextByte;
                }
                break;

            // 11: The next object is encoded in a special format. The remaining 6 bits indicate the format
            case 0b11:
                // Get the last 6 bits
                int format = firstByte & 0b00111111;
                // Handle special format based on the format variable
                // For now, just throw an exception to indicate unimplemented feature
                throw new NotImplementedException($"Special format {format} is not implemented.");
        }

        return length;
    }

    private static int ReadLength2(BinaryReader reader)
    {
        var firstByte = reader.ReadByte();

        if ((firstByte & 0x80) == 0)
        {
            // 1-byte length (0-63)
            return firstByte;
        }

        if ((firstByte & 0xC0) == 0x80)
        {
            // 2-byte length (64-16383)
            var secondByte = reader.ReadByte();
            return ((firstByte & 0x3F) << 8) | secondByte;
        }

        if ((firstByte & 0xC0) == 0xC0)
        {
            // 4-byte length (greater values)
            var bytes = reader.ReadBytes(4);
            // Assuming little-endian
            return BitConverter.ToInt32(bytes, 0);
        }

        throw new InvalidDataException("Invalid length encoding");
    }

    private void SkipValue(BinaryReader reader, byte type)
    {
        switch (type)
        {
            case 0x00: // String
                ReadString(reader);
                break;
            case 0x01: // List
            case 0x02: // Set
            case 0x03: // Sorted set
            case 0x04: // Hash
                int length = ReadLength(reader);
                for (int i = 0; i < length; i++)
                {
                    ReadString(reader); // Key
                    ReadString(reader); // Value
                }

                break;
            case 0x0C: // Quicklist (list of lists, used for Redis lists in RDB)
                int listLength = ReadLength(reader);
                for (int i = 0; i < listLength; i++)
                {
                    ReadString(reader);
                }

                break;
            case 0x0D: // Module
            case 0x0E: // Module 2
                SkipModule(reader);
                break;
            case 0x0F: // Stream
                SkipStream(reader);
                break;
            case (byte)RdbOpCode.ExpireTime:
            case (byte)RdbOpCode.ExpireTimeMs:
                reader.ReadBytes(8); // Skip 8 bytes of expiry data
                break;
            default:
                Console.WriteLine($"Unknown value type {type}. Skipping.");
                SkipUnknownValue(reader);
                break;
        }
    }

    private static void SkipUnknownValue(BinaryReader reader)
    {
        // In this example, we're just going to skip an arbitrary number of bytes
        // This should be replaced with logic specific to the type of unknown value
        // Adjust this logic based on actual requirements
        byte[] buffer = new byte[1024];
        int bytesRead;
        do
        {
            bytesRead = reader.Read(buffer, 0, buffer.Length);
        } while (bytesRead == buffer.Length);
    }

    private static void SkipModule(BinaryReader reader)
    {
        // Implementation for skipping module type
        // This needs to follow the specific structure of module data in Redis RDB files
    }

    private static void SkipStream(BinaryReader reader)
    {
        // Implementation for skipping stream type
        // This needs to follow the specific structure of stream data in Redis RDB files
    }
}

public enum RdbOpCode
{
    Eof = 0xFF,
    SelectDb = 0xFE,
    ExpireTime = 0xFD,
    ExpireTimeMs = 0xFC,
    ResizeDb = 0xFB,
    Aux = 0xFA
}