using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using codecrafters_redis.Cache;

namespace codecrafters_redis.Commands;

public class Xread : Base
{
    public override bool CanBePropagated => false;

    protected override Task OnMasterNodeExecute(Socket socket, int commandCount, string[] commandParts,
        bool replicaConnection = false)
    {
        var streamKeys = GetStreamKeys(commandParts);
        if (streamKeys.Count == 0)
        {
            socket.Send(Encoding.UTF8.GetBytes("*0\r\n"));
            return Task.CompletedTask;
        }

        var streamEntries = BuildStreamEntries(streamKeys);
        if (streamKeys.Count == 0)
        {
            socket.Send(Encoding.UTF8.GetBytes("*0\r\n"));
            return Task.CompletedTask;
        }

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
        socket.Send(Encoding.UTF8.GetBytes(response));

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
                        if (x.Timestamp == startTimestamp.Value && x.Sequence < startSequence.Value)
                        {
                            return false;
                        }
                        
                        if (x.Timestamp < startTimestamp.Value)
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

    private static List<StreamKeyWithEntryId> GetStreamKeys(string[] commandParts)
    {
        var streamKeys = new List<string>();
        var streamKeysWithEntryIds = new List<StreamKeyWithEntryId>();

        if (!string.Equals(commandParts[4], "streams", StringComparison.InvariantCultureIgnoreCase))
        {
            return streamKeysWithEntryIds;
        }

        streamKeys.AddRange(commandParts.Skip(5)
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

    protected override Task OnReplicaNodeExecute(Socket socket, int commandCount, string[] commandParts,
        bool replicaConnection = false)
    {
        return Task.CompletedTask;
    }
}

public record StreamKeyWithEntryId(string Key, string EntryId);