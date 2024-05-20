namespace codecrafters_redis.Cache;

public class StreamCacheItemValueItem
{
    public string Id { get; set; } = null!;
    public long Timestamp => long.Parse(Id.Split('-')[0]);
    public long Sequence => long.Parse(Id.Split('-')[1]);
    public List<StreamCacheItemValueItemValue> Value { get; set; } = null!;
}