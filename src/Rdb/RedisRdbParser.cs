using System.Text;

namespace codecrafters_redis.Rdb;

public class RedisRdbParser
{
    public static void ParseRdbFile(string filePath)
    {
        byte[] rdbFile = File.ReadAllBytes(filePath);
        int index = 0;

        // Read and validate the magic string
        string magicString = Encoding.ASCII.GetString(rdbFile, index, 5);
        if (magicString != "REDIS")
        {
            throw new InvalidDataException("Invalid RDB file: magic string mismatch.");
        }
        index += 5;

        // Read and validate the version number
        string versionString = Encoding.ASCII.GetString(rdbFile, index, 4);
        if (!int.TryParse(versionString, out int version))
        {
            throw new InvalidDataException("Unsupported RDB version.");
        }
        index += 4;

        // Parse the RDB file entries
        while (index < rdbFile.Length)
        {
            byte opCode = rdbFile[index++];

            if (opCode == 0xFA) // Auxiliary field
            {
                string auxKey = DecodeString(ref index, rdbFile);
                string auxValue = DecodeString(ref index, rdbFile);
                Console.WriteLine($"Aux Field: {auxKey} = {auxValue}");
            }
            else if (opCode == 0xFE) // Database selector
            {
                byte dbNumber = rdbFile[index++];
                Console.WriteLine($"Select DB: {dbNumber}");
            }
            else if (opCode == 0xFB) // Resizedb field
            {
                uint dbSize = DecodeLength(ref index, rdbFile);
                uint expireSize = DecodeLength(ref index, rdbFile);
                Console.WriteLine($"Resize DB: {dbSize}, Expire DB: {expireSize}");
            }
            else if (opCode == 0xFD) // Expiry time in seconds
            {
                uint expiryTime = DecodeUInt32(ref index, rdbFile);
                ParseKeyValue(ref index, rdbFile, expiryTime, false);
            }
            else if (opCode == 0xFC) // Expiry time in milliseconds
            {
                ulong expiryTimeMs = DecodeUInt64(ref index, rdbFile);
                ParseKeyValue(ref index, rdbFile, expiryTimeMs, true);
            }
            else if (opCode == 0xFF) // End of RDB file
            {
                byte[] checksum = new byte[8];
                Array.Copy(rdbFile, index, checksum, 0, 8);
                index += 8;
                Console.WriteLine("End of RDB file. Checksum: " + BitConverter.ToString(checksum));
                break;
            }
            else // Key-value pair without expiry
            {
                index--; // Step back to include opCode in the key-value processing
                ParseKeyValue(ref index, rdbFile, null, null);
            }
        }
    }

    private static string DecodeString(ref int index, byte[] bytes)
    {
        uint length = DecodeLength(ref index, bytes);
        string result = Encoding.UTF8.GetString(bytes, index, (int)length);
        index += (int)length;
        return result;
    }

    private static uint DecodeLength(ref int index, byte[] bytes)
    {
        int i = index;
        uint encodingType = (uint)bytes[i] >> 6;
        if (encodingType == 0b00)
        {
            index += 1;
            return bytes[i];
        }
        if (encodingType == 0b01)
        {
            index += 2;
            return (((uint)(bytes[i] & 0b11_1111) << 8) | bytes[i + 1]);
        }
        if (encodingType == 0b10)
        {
            index += 5;
            return (uint)((bytes[i + 1] << 24) + (bytes[i + 2] << 16) +
                          (bytes[i + 3] << 08) + (bytes[i + 4] << 00));
        }
        if (encodingType == 0b11)
        {
            throw new NotSupportedException(
                "Length encoding starts with 11, indicating special format, which is currently not supported.");
        }
        throw new InvalidDataException(
            "Reached a part of the code that should not be reachable.");
    }

    private static uint DecodeUInt32(ref int index, byte[] bytes)
    {
        uint result = (uint)((bytes[index] << 24) + (bytes[index + 1] << 16) +
                             (bytes[index + 2] << 08) + (bytes[index + 3]));
        index += 4;
        return result;
    }

    private static ulong DecodeUInt64(ref int index, byte[] bytes)
    {
        ulong result = (ulong)((bytes[index] << 56) + (bytes[index + 1] << 48) +
                               (bytes[index + 2] << 40) + (bytes[index + 3] << 32) +
                               (bytes[index + 4] << 24) + (bytes[index + 5] << 16) +
                               (bytes[index + 6] << 08) + (bytes[index + 7]));
        index += 8;
        return result;
    }

    private static void ParseKeyValue(ref int index, byte[] bytes, object expiryTime, bool? isMs)
    {
        byte valueType = bytes[index++];
        string key = DecodeString(ref index, bytes);
        string value = DecodeString(ref index, bytes);

        if (expiryTime != null)
        {
            if (isMs == true)
            {
                Console.WriteLine($"Key-Value Pair with Expiry (ms): {key} = {value}, Expiry: {expiryTime}ms");
            }
            else
            {
                Console.WriteLine($"Key-Value Pair with Expiry (s): {key} = {value}, Expiry: {expiryTime}s");
            }
        }
        else
        {
            Console.WriteLine($"Key-Value Pair: {key} = {value}");
        }
    }

    public static void Main()
    {
        ParseRdbFile("path_to_rdb_file.rdb");
    }
}