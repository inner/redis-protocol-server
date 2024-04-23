using System.Collections.Concurrent;

namespace codecrafters_redis;

public static class DataCache
{
    private static ConcurrentDictionary<string, CacheItem> Cache { get; } = new();
    
    public static void Add(string key, string value, long? expiry = null)
    {
        Cache[key] = new CacheItem
        {
            Key = key,
            Value = value,
            Expiry = expiry
        };
    }
    
    public static string? Get(string key)
    {
        return Cache.TryGetValue(key, out var cacheItem)
            ? cacheItem.Value
            : null;
    }
}

public class CacheItem
{
    public string Key { get; set; }
    public string? Value { get; set; }
    public long? Expiry { get; set; }
}