namespace codecrafters_redis.Rdb;

public static class RdbConstants
{
    public const int One = 1;
    public const int Two = 2;
    public const int Four = 4;
    public const int Eight = 8;

    public static class Verification
    {
        public const int RedisMagicStringBytes = 5;
        public const string RedisMagicString = "REDIS";
        public const int RedisVersionBytes = 4;
        public const int Checksum = 8;
    }

    public static class OpCodes
    {
        public const int Function2 = 245;
        public const int Function = 246;
        public const int ModuleAux = 247;
        public const int Idle = 248;
        public const int Freq = 249;
        public const int Aux = 0xFA;
        public const int ResizeDb = 0xFB;
        public const int ExpireTimeMs = 0xFC;
        public const int ExpireTime = 0xFD;
        public const int SelectDb = 0xFE;
        public const int Eof = 0xFF;
    }

    public static class LengthEncoding
    {
        public const int Bit6 = 0;
        public const int Bit14 = 1;
        public const int Bit32 = 0x80;
        public const int Bit64 = 0x81;
        public const int EncVal = 3;
    }

    public static class EncType
    {
        public const uint Int8 = 0;
        public const uint Int16 = 1;
        public const uint Int32 = 2;
        public const uint Lzf = 3;
    }

    public static class DataTypes
    {
        public const int String = 0;
        public const int List = 1;
        public const int Set = 2;
        public const int Zset = 3;
        public const int Hash = 4;

        /* >= 4.0 */
        public const int Zset2 = 5;
        public const int Module = 6;
        public const int Module2 = 7;

        public const int HashZipmap = 9;
        public const int ListZiplist = 10;
        public const int SetIntset = 11;
        public const int ZsetZiplist = 12;
        public const int HashZiplist = 13;

        /* >= 3.2 */
        public const int ListQuicklist = 14;

        /* >= 5.0 */
        public const int StreamListPacks = 15;

        /* >= 7.0 */
        public const int HashListPack = 16;
        public const int ZsetListPack = 17;
        public const int ListQuicklist2 = 18;
        public const int StreamListPacks2 = 19;

        public static readonly Dictionary<int, string> Mapping = new()
        {
            { 0, "string" },
            { 1, "list" },
            { 2, "set" },
            { 3, "sortedset" },
            { 4, "hash" },
            { 5, "sortedset" },
            { 6, "module" },
            { 7, "module" },
            { 9, "hash" },
            { 10, "list" },
            { 11, "set" },
            { 12, "sortedset" },
            { 13, "hash" },
            { 14, "list" },
            { 15, "stream" },
            { 16, "hash" },
            { 17, "sortedset" },
            { 18, "list" },
            { 19, "stream" }
        };
    }
}