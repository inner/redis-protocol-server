using System.Collections.Concurrent;
using System.Text.Json;

namespace codecrafters_redis;

public static class DataCache
{
    private static ConcurrentDictionary<string, string> Cache { get; } = new();

    public static void Set(string key, string value, int? expiry = null)
    {
        var basicCacheItem = new BasicCacheItem
        {
            Value = value,
            Expiry = expiry.HasValue
                ? DateTime.Now.AddMilliseconds(expiry.Value)
                : null
        };

        var serialized = JsonSerializer.Serialize(basicCacheItem);
        Cache[key] = serialized;
    }

    public static BasicCacheItem? Get(string key)
    {
        BasicCacheItem? basicCacheItem = null;
        if (!Cache.TryGetValue(key, out var basicCacheItemSerialized))
        {
            return basicCacheItem;
        }

        basicCacheItem = JsonSerializer.Deserialize<BasicCacheItem>(basicCacheItemSerialized);
        if (basicCacheItem == null)
        {
            return null;
        }

        if (!basicCacheItem.Expiry.HasValue)
        {
            return basicCacheItem;
        }

        return basicCacheItem.Expiry.Value < DateTime.Now
            ? null
            : basicCacheItem;
    }
    
    public static string Xadd(string key, IDictionary<string, string> value)
    {
        var id = value.Single(x => x.Key == "Id").Value;
        
        var streamCacheItem = new StreamCacheItem
        {
            Key = key,
            Value = value
        };

        var serialized = JsonSerializer.Serialize(streamCacheItem);
        Cache[key] = serialized;
        return id;
    }

    public static string? Fetch(string key)
    {
        Cache.TryGetValue(key, out var value);
        return value;
    }
}

public class BasicCacheItem : ICacheItemBase, IExpiredCacheItem
{
    public required string Value { get; set; }
    public string Type => nameof(BasicCacheItem);
    public DateTime? Expiry { get; set; }
}

public class StreamCacheItem : ICacheItemBase
{
    public required string Key { get; set; }
    public required IDictionary<string, string> Value { get; set; }
    public string Type => nameof(StreamCacheItem);
}

public interface IExpiredCacheItem
{
    public DateTime? Expiry { get; set; }
}

public interface ICacheItemBase
{
    public string Type { get; }
}