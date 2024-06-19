namespace codecrafters_redis.Rdb;

public class RdbReaderException : Exception
{
    public RdbReaderException(string message) : base(message)
    { }
}