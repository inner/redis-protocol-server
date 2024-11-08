namespace codecrafters_redis.Cache;

public class BasicCacheItem : ICacheItemBase, IExpiredCacheItem
{
    public required string Value { get; init; }
    public string Type => nameof(BasicCacheItem);
    public long Expiry { get; set; }
}