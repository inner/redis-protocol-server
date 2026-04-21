namespace Redis.Cache;

public class StreamCacheItem : ICacheItem
{
    public required List<StreamCacheItemValueItem> Value { get; set; }
    public string Type => nameof(StreamCacheItem);
}