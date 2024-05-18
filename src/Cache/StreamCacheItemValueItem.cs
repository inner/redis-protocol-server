namespace codecrafters_redis.Cache;

public class StreamCacheItemValueItem
{
    public string Id { get; set; } = null!;
    public long IdTimestamp => long.Parse(Id.Split('-')[0]);
    public long IdSequence => long.Parse(Id.Split('-')[1]);
    public List<StreamCacheItemValueItemValue> Value { get; set; } = null!;
}