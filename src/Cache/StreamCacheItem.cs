namespace Redis.Cache;

public class StreamCacheItem : ICacheItemBase
{
    public required List<StreamCacheItemValueItem> Value { get; set; }
    public string Type => nameof(StreamCacheItem);
}