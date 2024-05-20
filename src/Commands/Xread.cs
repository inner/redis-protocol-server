using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using codecrafters_redis.Cache;

namespace codecrafters_redis.Commands;

public record StreamKeyWithEntryId(string Key, string EntryId);

public class Xread : Base
{
    public override bool CanBePropagated => false;

    protected override async Task OnMasterNodeExecute(Socket socket, int commandCount, string[] commandParts,
        bool replicaConnection = false)
    {
        var blockIndex = Array.IndexOf(commandParts, "block") + 1;
        if (blockIndex != -1 && int.TryParse(commandParts[blockIndex + 1], out var blockTime))
        {
            await Task.Delay(blockTime);
        }

        if (blockIndex != -1 && (commandParts[6] == "\\x00" || commandParts[6] == "0"))
        {
            await GenerateCommonResponse(socket, commandParts, noWaitTimeout: true, replicaConnection);
        }
        else
        {
            await GenerateCommonResponse(socket, commandParts, replicaConnection);
        }
    }

    protected override async Task OnReplicaNodeExecute(Socket socket, int commandCount, string[] commandParts,
        bool replicaConnection = false)
    {
        var blockIndex = Array.IndexOf(commandParts, "block") + 1;
        if (blockIndex != -1 && int.TryParse(commandParts[blockIndex + 1], out var blockTime))
        {
            await Task.Delay(blockTime);
        }

        if (blockIndex != -1 && (commandParts[6] == "\\x00" || commandParts[6] == "0"))
        {
            await GenerateCommonResponse(socket, commandParts, noWaitTimeout: true, replicaConnection);
        }
        else
        {
            await GenerateCommonResponse(socket, commandParts, replicaConnection);
        }
    }

    private static Task GenerateCommonResponse(Socket socket, string[] commandParts, bool noWaitTimeout = false,
        bool replicaConnection = false)
    {
        var isBlocking = Array.IndexOf(commandParts, "block") != -1;
        List<StreamCacheItemValueItem> streamEntries = [];

        if (noWaitTimeout)
        {
            while (!streamEntries.Any())
            {
                var streamKeys = GetStreamKeys(commandParts, isBlocking);
                if (streamKeys.Count == 0)
                {
                    continue;
                }
                
                streamEntries.AddRange(BuildStreamEntries(streamKeys));
            }
        }
        else
        {
            var streamKeys = GetStreamKeys(commandParts, isBlocking);
            if (streamKeys.Count == 0)
            {
                if (!replicaConnection)
                {
                    socket.Send(Encoding.UTF8.GetBytes("$-1\r\n"));
                }
        
                return Task.CompletedTask;
            }
            
            streamEntries.AddRange(BuildStreamEntries(streamKeys));
        }
        
        if (streamEntries.Count == 0)
        {
            if (!replicaConnection)
            {
                socket.Send(Encoding.UTF8.GetBytes("$-1\r\n"));
            }

            return Task.CompletedTask;
        }

        var sb = new StringBuilder();

        sb.Append($"*{streamEntries.Count}\r\n");
        foreach (var streamEntry in streamEntries)
        {
            sb.Append("*2\r\n");
            sb.Append($"${streamEntry.Key.Length}\r\n{streamEntry.Key}\r\n");
            sb.Append("*1\r\n");
            sb.Append("*2\r\n");
            sb.Append($"${streamEntry.Id.Length}\r\n{streamEntry.Id}\r\n");
            sb.Append($"*{streamEntry.Value.Count}\r\n");

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

    private static List<StreamCacheItemValueItem> BuildStreamEntries(List<StreamKeyWithEntryId> streamKeys)
    {
        var streamEntries = new List<StreamCacheItemValueItem>();

        foreach (var streamKey in streamKeys)
        {
            var fetchItem = DataCache.Fetch(streamKey.Key);

            if (string.IsNullOrEmpty(fetchItem))
            {
                return streamEntries;
            }

            var streamCacheItem = fetchItem.Deserialize<StreamCacheItem>();
            if (streamCacheItem == null)
            {
                return streamEntries;
            }

            long? startTimestamp = null;
            long? startSequence = null;

            if (Regex.IsMatch(streamKey.EntryId, @"^\d+-\d+$"))
            {
                startTimestamp = long.Parse(streamKey.EntryId.Split('-')[0]);
                startSequence = long.Parse(streamKey.EntryId.Split('-')[1]);
            }
            else if (long.TryParse(streamKey.EntryId, out var startEntryIdNumber))
            {
                startTimestamp = startEntryIdNumber;
            }

            streamEntries.AddRange(streamCacheItem.Value
                .Where(x =>
                {
                    if (startTimestamp.HasValue && startSequence.HasValue)
                    {
                        if (x.Timestamp < startTimestamp.Value)
                        {
                            return false;
                        }

                        if (x.Timestamp == startTimestamp.Value && x.Sequence <= startSequence.Value)
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

                    return true;
                }));
        }

        return streamEntries;
    }

    private static List<StreamKeyWithEntryId> GetStreamKeys(string[] commandParts, bool isBlocking)
    {
        var streamKeys = new List<string>();
        var streamKeysWithEntryIds = new List<StreamKeyWithEntryId>();

        if (!string.Equals(commandParts[isBlocking ? 8 : 4], "streams", StringComparison.InvariantCultureIgnoreCase))
        {
            return streamKeysWithEntryIds;
        }

        streamKeys.AddRange(commandParts.Skip(isBlocking ? 9 : 5)
            .Where(x => !Regex.IsMatch(x, @"^\$\d+$") && !Regex.IsMatch(x, @"^\d+-\d+$")));

        var streamKeyEntryIds = new List<string>();
        var index = commandParts.ToList().IndexOf(streamKeys.Last());
        for (var i = index + 1; i < commandParts.Length; i++)
        {
            if (Regex.IsMatch(commandParts[i], @"^\d+-\d+$"))
            {
                streamKeyEntryIds.Add(commandParts[i]);
            }
        }

        for (var i = 0; i < streamKeys.Count; i++)
        {
            if (streamKeyEntryIds.Count > i)
            {
                streamKeysWithEntryIds.Add(new StreamKeyWithEntryId(streamKeys[i], streamKeyEntryIds[i]));
            }
        }

        return streamKeysWithEntryIds;
    }
}