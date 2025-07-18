namespace Redis.Cache;

public class StreamCacheItemValueItem
{
    public string Id { get; set; } = null!;
    public string Key { get; set; } = null!;
    public long Timestamp => long.Parse(Id.Split('-')[0]);
    public long Sequence => long.Parse(Id.Split('-')[1]);
    public List<StreamCacheItemValueItemValue> Value { get; set; } = null!;
    public string[] Flattened => Value.SelectMany(x => new[] { x.Key, x.Value }).ToArray();
}