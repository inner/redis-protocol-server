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
        string result;
        var key = commandContext.CommandDetails.CommandParts[4];

        var fetchItem = DataCache.Fetch(key);

        if (string.IsNullOrEmpty(fetchItem))
        {
            result = RespBuilder.Null();
            commandContext.Socket.SendCommand(result);
            return Task.FromResult(result);
        }

        var streamCacheItem = fetchItem.Deserialize<StreamCacheItem>();
        if (streamCacheItem == null)
        {
            result = RespBuilder.Null();
            commandContext.Socket.SendCommand(result);
            return Task.FromResult(result);
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

        var sb = new StringBuilder($"*{streamEntries.Count}\r\n");
        foreach (var streamEntry in streamEntries)
        {
            sb.Append($"*{streamEntry.Value.Count}\r\n");
            sb.Append($"${streamEntry.Id.Length}\r\n{streamEntry.Id}\r\n");
            sb.Append($"*{streamEntry.Value.Count * 2}\r\n");
            foreach (var cacheItemValueItemValue in streamEntry.Value)
            {
                sb.Append($"${cacheItemValueItemValue.Key.Length}\r\n{cacheItemValueItemValue.Key}\r\n");
                sb.Append($"${cacheItemValueItemValue.Value.Length}\r\n{cacheItemValueItemValue.Value}\r\n");
            }
        }

        result = sb.ToString();

        if (!commandContext.ReplicaConnection)
        {
            commandContext.Socket.SendCommand(result);
        }

        return Task.FromResult(result);
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