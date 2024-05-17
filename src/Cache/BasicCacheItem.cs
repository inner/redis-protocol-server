namespace codecrafters_redis.Cache;

public class BasicCacheItem : ICacheItemBase, IExpiredCacheItem
{
    public required string Value { get; set; }
    public string Type => nameof(BasicCacheItem);
    public DateTime? Expiry { get; set; }
}