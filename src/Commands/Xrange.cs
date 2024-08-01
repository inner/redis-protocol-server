using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using codecrafters_redis.Cache;
using codecrafters_redis.Common;
using codecrafters_redis.Receivers;

namespace codecrafters_redis.Commands;

public class Xrange : Base
{
    public override bool CanBePropagated => false;

    protected override Task OnMasterNodeExecute(Socket socket, CommandDetails commandDetails,
        List<CommandQueueItem> commandQueue, ReceiverBase receiver, bool replicaConnection = false)
    {
        return GenerateCommonResponse(socket, commandDetails, replicaConnection);
    }

    protected override Task OnReplicaNodeExecute(Socket socket, CommandDetails commandDetails,
        List<CommandQueueItem> commandQueue, ReceiverBase receiver, bool replicaConnection = false)
    {
        return GenerateCommonResponse(socket, commandDetails, replicaConnection);
    }
    
    private static Task GenerateCommonResponse(Socket socket, CommandDetails commandDetails, bool replicaConnection = false)
    {
        var key = commandDetails.CommandParts[4];

        var fetchItem = DataCache.Fetch(key);

        if (string.IsNullOrEmpty(fetchItem))
        {
            socket.Send(Encoding.UTF8.GetBytes("$-1\r\n"));
            return Task.CompletedTask;
        }

        var streamCacheItem = fetchItem.Deserialize<StreamCacheItem>();
        if (streamCacheItem == null)
        {
            socket.Send(Encoding.UTF8.GetBytes("$-1\r\n"));
            return Task.CompletedTask;
        }

        var startEntryId = commandDetails.CommandParts[6];
        var endEntryId = commandDetails.CommandParts[8];

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

        var sb = new StringBuilder();
        
        sb.Append($"*{streamEntries.Count}\r\n");
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

        var response = sb.ToString();
        
        if (!replicaConnection)
        {
            socket.Send(Encoding.UTF8.GetBytes(response));   
        }
        
        return Task.CompletedTask;
    }
}