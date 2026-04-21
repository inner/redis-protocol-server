namespace Redis.Cache;

public class BasicCacheItem : ICacheItem, IExpiredCacheItem
{
    public required string Value { get; init; }
    public string Type => nameof(BasicCacheItem);
    public long Expiry { get; set; }
}