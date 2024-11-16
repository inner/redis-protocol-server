using System.Collections.Concurrent;
using System.Text.Json;
using codecrafters_redis.Common;

namespace codecrafters_redis.Cache;

public static class DataCache
{
    private static ConcurrentDictionary<string, string> Cache { get; } = new();

    public static void Set(string key, string value, long? expiry = null)
    {
        var basicCacheItem = new BasicCacheItem
        {
            Value = value,
            Expiry = expiry is > 0
                ? expiry.Value
                : DateTimeOffset.MaxValue.ToUnixTimeMilliseconds()
        };

        var serialized = JsonSerializer.Serialize(basicCacheItem);
        Cache[key] = serialized;
    }

    public static BasicCacheItem? Get(string key)
    {
        if (!Cache.TryGetValue(key, out var basicCacheItemSerialized))
        {
            return null;
        }

        var basicCacheItem = JsonSerializer.Deserialize<BasicCacheItem>(basicCacheItemSerialized);
        if (basicCacheItem?.Expiry == null)
        {
            return null;
        }

        return basicCacheItem.Expiry < DateTimeOffset.Now.ToUnixTimeMilliseconds()
            ? null
            : basicCacheItem;
    }

    public static IList<string> GetKeys(string? pattern = null)
    {
        if (pattern is null)
        {
            return Cache.Select(x => x.Key).ToList();
        }

        pattern = pattern.Replace("*", string.Empty);
        
        return Cache
            .Where(x => x.Key.StartsWith(pattern))
            .Select(x => x.Key)
            .ToList();
    }

    public static string Xadd(string key, StreamCacheItemValueItem value)
    {
        var entryId = value.Id;

        var fetchItem = Fetch(key);
        if (!string.IsNullOrEmpty(fetchItem))
        {
            var existingStreamCacheItem = fetchItem.Deserialize<StreamCacheItem>();
            if (existingStreamCacheItem != null)
            {
                existingStreamCacheItem.Value.Add(value);
            }

            Cache[key] = JsonSerializer.Serialize(existingStreamCacheItem);
        }
        else
        {
            var streamCacheItem = new StreamCacheItem
            {
                Value = [value]
            };

            Cache[key] = JsonSerializer.Serialize(streamCacheItem);
        }

        return entryId;
    }

    public static string? Fetch(string key)
    {
        Cache.TryGetValue(key, out var value);
        return value;
    }
}