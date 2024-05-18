namespace codecrafters_redis.Cache;

public class StreamCacheItemValueItem
{
    public string Id { get; set; } = null!;
    public long IdTimestamp => long.Parse(Id.Split('-')[0]);
    public long IdSequence => long.Parse(Id.Split('-')[1]);
    public string Key { get; set; } = null!;
    public string Value { get; set; } = null!;
}