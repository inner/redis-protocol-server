namespace codecrafters_redis.Cache;

public interface IExpiredCacheItem
{
    public DateTime? Expiry { get; set; }
}