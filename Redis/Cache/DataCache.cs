using System.Collections.Concurrent;
using System.Text.Json;
using Redis.Common;

namespace Redis.Cache;

// article for refactoring
// https://stackoverflow.com/questions/9625246/what-are-the-underlying-data-structures-used-for-redis
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

    public static int Rpush(string listKey, params string[] listValues)
    {
        var listItem = Fetch(listKey);
        List<string> list = [];

        if (string.IsNullOrEmpty(listItem))
        {
            list.AddRange(listValues);
            Cache[listKey] = JsonSerializer.Serialize(list);
            return list.Count;
        }

        list = listItem.Deserialize<List<string>>() ?? [];
        list.AddRange(listValues);
        Cache[listKey] = JsonSerializer.Serialize(list);
        return list.Count;
    }
    
    public static IList<string> Lrange(string listKey, int start, int end)
    {
        var listItem = Fetch(listKey);
        
        if (string.IsNullOrEmpty(listItem))
        {
            return [];
        }

        var list = listItem.Deserialize<List<string>>() ?? [];
        if (start < 0)
        {
            start += list.Count;
        }

        if (end < 0)
        {
            end += list.Count;
        }

        return list
            .Skip(start)
            .Take(end - start + 1)
            .ToList();
    }

    public static string? Fetch(string key)
    {
        Cache.TryGetValue(key, out var value);
        return value;
    }

    public static int CountKeys(params string[] keys)
    {
        return Cache.Count(x => keys.Distinct().Contains(x.Key));
    }

    public static int DelKeys(params string[] keys)
    {
        return keys.Count(key => Cache.TryRemove(key, out _));
    }
}