using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text.Json;
using Redis.Common;

namespace Redis.Cache;

// article for refactoring
// https://stackoverflow.com/questions/9625246/what-are-the-underlying-data-structures-used-for-redis
public static class DataCache
{
    private static ConcurrentDictionary<string, string> Cache { get; } = new();
    private static ConcurrentDictionary<string, List<Socket>> Subscriptions { get; } = new();

    public static void AddSubscription(string channel, Socket socket)
    {
        if (!Subscriptions.TryGetValue(channel, out var value))
        {
            value = [];
            Subscriptions[channel] = value;
        }

        if (!value.Contains(socket))
        {
            value.Add(socket);
        }
    }

    public static int GetSubscriptions(string channel)
    {
        return Subscriptions.TryGetValue(channel, out var value)
            ? value.Count
            : 0;
    }
    
    public static void RemoveSubscription(string channel, Socket socket)
    {
        if (Subscriptions.TryGetValue(channel, out var sockets))
        {
            sockets.Remove(socket);
            if (sockets.Count == 0)
            {
                Subscriptions.TryRemove(channel, out _);
            }
        }
    }

    public static void Publish(string channel, string message)
    {
        if (!Subscriptions.TryGetValue(channel, out var sockets))
        {
            return;
        }
        
        Parallel.ForEach(sockets, socket =>
        {
            try
            {
                if (socket.Connected)
                {
                    socket.Send(message.AsBytes());
                }
            }
            catch (SocketException)
            {
                Subscriptions[channel].Remove(socket);
            }
            catch (ObjectDisposedException)
            {
                Subscriptions[channel].Remove(socket);
            }
        });
    }

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

    public static int Lpush(string listKey, params string[] listValues)
    {
        var listItem = Fetch(listKey);
        List<string> list = [];
        var reversed = listValues.Reverse();

        if (string.IsNullOrEmpty(listItem))
        {
            list.AddRange(reversed);
            Cache[listKey] = JsonSerializer.Serialize(list);
            return list.Count;
        }

        list = listItem.Deserialize<List<string>>() ?? [];
        list.InsertRange(0, reversed);
        Cache[listKey] = JsonSerializer.Serialize(list);
        return list.Count;
    }

    public static IList<string> Lrange(string listKey, int start, int stop)
    {
        var listItem = Fetch(listKey);

        if (string.IsNullOrEmpty(listItem))
            return [];

        var list = listItem.Deserialize<List<string>>() ?? [];

        if (start >= list.Count)
            return [];

        if (start < 0)
            start = list.Count + start;

        if (stop >= list.Count)
            stop = list.Count - 1;

        if (stop < 0)
            stop = list.Count + stop;

        return list
            .Skip(start)
            .Take(stop - start + 1)
            .ToList();
    }

    public static int Llen(string listKey)
    {
        var listItem = Fetch(listKey);

        if (string.IsNullOrEmpty(listItem))
            return 0;

        var list = listItem.Deserialize<List<string>>() ?? [];
        return list.Count;
    }

    public static string[] Lpop(string listKey, int? count = null)
    {
        var listItem = Fetch(listKey);

        if (string.IsNullOrEmpty(listItem))
            return [];

        var list = listItem.Deserialize<List<string>>() ?? [];
        if (list.Count == 0)
            return [];

        string[] values;

        if (count == null)
        {
            values = [list[0]];
            list.RemoveAt(0);
        }
        else
        {
            var itemsToRemove = Math.Min(count.Value, list.Count);

            values = list.Take(itemsToRemove)
                .ToArray();

            list.RemoveRange(0, itemsToRemove);
        }

        Cache[listKey] = JsonSerializer.Serialize(list);
        return values;
    }

    public static async Task<string[]> Blpop(string listKey, double timeout = 0.0)
    {
        var listItem = Fetch(listKey);

        if (string.IsNullOrEmpty(listItem))
        {
            if (timeout == 0)
            {
                while (string.IsNullOrEmpty(listItem))
                {
                    await Task.Delay(5);
                    listItem = Fetch(listKey);
                }
            }
            else
            {
                var startTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                while (string.IsNullOrEmpty(listItem) &&
                       DateTimeOffset.Now.ToUnixTimeMilliseconds() - startTime < timeout * 1000)
                {
                    await Task.Delay(5);
                    listItem = Fetch(listKey);
                }
            }
        }

        if (string.IsNullOrEmpty(listItem))
            return [];

        var list = listItem.Deserialize<List<string>>() ?? [];
        if (list.Count == 0)
            return [];

        var value = list[0];
        list.RemoveAt(0);
        Cache[listKey] = JsonSerializer.Serialize(list);
        return [listKey, value];
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