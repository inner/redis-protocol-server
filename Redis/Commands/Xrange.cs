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

        if (Regex.IsMatch(startEntryId, @"^\d+-\d+$"))
        {
            startTimestamp = long.Parse(startEntryId.Split('-')[0]);
            startSequence = long.Parse(startEntryId.Split('-')[1]);
        }
        else if (long.TryParse(startEntryId, out var startEntryIdNumber))
        {
            startTimestamp = startEntryIdNumber;
        }

        if (Regex.IsMatch(endEntryId, @"^\d+-\d+$"))
        {
            endTimestamp = long.Parse(endEntryId.Split('-')[0]);
            endSequence = long.Parse(endEntryId.Split('-')[1]);
        }
        else if (long.TryParse(endEntryId, out var endEntryIdNumber))
        {
            endTimestamp = endEntryIdNumber;
        }

        var streamEntries = streamCacheItem.Value
            .Where(x =>
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

                return true;
            })
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
}