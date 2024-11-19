namespace Redis.Rdb;

public static class RdbConstants
{
    public const int One = 1;
    public const int Two = 2;
    public const int Four = 4;

    public static class Verification
    {
        public const int RedisMagicStringBytes = 5;
        public const string RedisMagicString = "REDIS";
        public const int RedisVersionBytes = 4;
        public const int Checksum = 8;
    }

    public static class OpCodes
    {
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
    }
}