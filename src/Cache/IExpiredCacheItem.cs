namespace codecrafters_redis.Cache;

public interface IExpiredCacheItem
{
    public long Expiry { get; set; }
}