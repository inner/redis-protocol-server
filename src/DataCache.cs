using System.Collections.Concurrent;

namespace codecrafters_redis;

public static class DataCache
{
    private static ConcurrentDictionary<string, CacheItem> Cache { get; } = new();
    
    public static void Set(string key, string value, int? expiry = null)
    {
        Cache[key] = new CacheItem
        {
            Key = key,
            Value = value,
            Expiry = expiry.HasValue
                ? DateTime.Now.AddMilliseconds(expiry.Value)
                : null
        };
    }
    
    public static CacheItem? Get(string key)
    {
        return Cache.TryGetValue(key, out var cacheItem)
            ? cacheItem.Expiry.HasValue && cacheItem.Expiry.Value < DateTime.Now
                ? null
                : cacheItem
            : null;
    }
}

public class CacheItem
{
    public required string Key { get; set; }
    public string? Value { get; set; }
    public DateTime? Expiry { get; set; }
}