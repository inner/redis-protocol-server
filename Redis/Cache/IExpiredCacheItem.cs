namespace Redis.Cache;

public interface IExpiredCacheItem
{
    public long Expiry { get; set; }
}