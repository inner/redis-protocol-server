using System.Text;
using System.Text.RegularExpressions;
using Redis.Cache;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Xrange : Base
{
    protected override string Name => nameof(Xrange);
    public override bool CanBePropagated => false;

    protected override async Task<string> OnMasterNodeExecute(CommandContext commandContext)
    {
        return await GenerateCommonResponse(commandContext);
    }

    protected override async Task<string> OnReplicaNodeExecute(CommandContext commandContext)
    {
        return await GenerateCommonResponse(commandContext);
    }

    private static Task<string> GenerateCommonResponse(CommandContext commandContext)
    {
        string resp;
        var key = commandContext.CommandDetails.CommandParts[4];

        var fetchItem = DataCache.Fetch(key);

        if (string.IsNullOrEmpty(fetchItem))
        {
            resp = RespBuilder.Null();
            commandContext.Socket.SendCommand(resp);
            return Task.FromResult(resp);
        }

        var streamCacheItem = fetchItem.Deserialize<StreamCacheItem>();
        if (streamCacheItem == null)
        {
            resp = RespBuilder.Null();
            commandContext.Socket.SendCommand(resp);
            return Task.FromResult(resp);
        }

        var startEntryId = commandContext.CommandDetails.CommandParts[6];
        var endEntryId = commandContext.CommandDetails.CommandParts[8];

        long? startTimestamp = null;
        long? startSequence = null;
        long? endTimestamp = null;
        long? endSequence = null;
        
        const string entryIdPattern = @"^\d+-\d+$";
        
        if (Regex.IsMatch(startEntryId, entryIdPattern))
        {
            startTimestamp = long.Parse(startEntryId.Split('-')[0]);
            startSequence = long.Parse(startEntryId.Split('-')[1]);
        }
        else if (long.TryParse(startEntryId, out var startEntryIdNumber))
        {
            startTimestamp = startEntryIdNumber;
        }

        if (Regex.IsMatch(endEntryId, entryIdPattern))
        {
            endTimestamp = long.Parse(endEntryId.Split('-')[0]);
            endSequence = long.Parse(endEntryId.Split('-')[1]);
        }
        else if (long.TryParse(endEntryId, out var endEntryIdNumber))
        {
            endTimestamp = endEntryIdNumber;
        }

        var streamEntries = streamCacheItem.Value
            .Where(StreamEntriesFilter(startTimestamp, startSequence, endTimestamp, endSequence))
            .ToList();

        resp = BuildResp(streamEntries);
        commandContext.Socket.SendCommand(resp);

        return Task.FromResult(resp);
    }

    private static string BuildResp(List<StreamCacheItemValueItem> streamEntries)
    {
        var sb = new StringBuilder(RespBuilder.InitArray(streamEntries.Count));
        foreach (var streamEntry in streamEntries)
        {
            sb.Append(RespBuilder.InitArray(2));
            sb.Append(RespBuilder.BulkString(streamEntry.Id));
            sb.Append(RespBuilder.InitArray(streamEntry.Flattened.Length));
            for (var i = 0; i < streamEntry.Flattened.Length; i += 2)
            {
                sb.Append(RespBuilder.BulkString(streamEntry.Flattened[i]));
                sb.Append(RespBuilder.BulkString(streamEntry.Flattened[i + 1]));
            }
        }
        
        return sb.ToString();
    }

    private static Func<StreamCacheItemValueItem, bool> StreamEntriesFilter(long? startTimestamp, long? startSequence,
        long? endTimestamp, long? endSequence)
    {
        return x =>
        {
            if (startTimestamp.HasValue && startSequence.HasValue)
            {
                if (x.Timestamp < startTimestamp.Value)
                {
                    return false;
                }

                if (x.Timestamp == startTimestamp.Value && x.Sequence < startSequence.Value)
                {
                    return false;
                }
            }
            else if (startTimestamp.HasValue && !startSequence.HasValue)
            {
                if (x.Timestamp < startTimestamp.Value)
                {
                    return false;
                }
            }

            if (endTimestamp.HasValue && endSequence.HasValue)
            {
                if (x.Timestamp > endTimestamp.Value)
                {
                    return false;
                }

                if (x.Timestamp == endTimestamp.Value && x.Sequence > endSequence.Value)
                {
                    return false;
                }
            }
            else if (endTimestamp.HasValue && !endSequence.HasValue)
            {
                if (x.Timestamp > endTimestamp.Value)
                {
                    return false;
                }
            }

            return true;
        };
    }
    
    public override Dictionary<string, Dictionary<string, string>> Docs()
    {
        return new()
        {
            {
                Name,
                new()
                {
                    { "summary", "Returns the messages from a stream within a range of IDs." },
                    { "usage #1", "redis-cli XADD weather_in_london 1-0 temperature 20 humidity 95" },
                    { "usage #2", "redis-cli XADD weather_in_london 1-* temperature 19 humidity 70" },
                    { "usage #3", "redis-cli XADD weather_in_london 2-* temperature 24 humidity 78" },
                    { "usage #4", "redis-cli XADD weather_in_london 2-* temperature 24 humidity 78" },
                    { "usage #5", "redis-cli XADD weather_in_london * temperature 25 humidity 90" },
                    { "usage #6", "redis-cli XRANGE weather_in_london 1 1" },
                    { "usage #7", "redis-cli XRANGE weather_in_london 1 1-1" },
                    { "usage #8", "redis-cli XRANGE weather_in_london - 1" },
                    { "usage #9", "redis-cli XRANGE weather_in_london - 1-0" },
                    { "usage #10", "redis-cli XRANGE weather_in_london 2 +" }
                }
            }
        };
    }
}