using System.Text;
using System.Text.RegularExpressions;
using Redis.Cache;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public record StreamKeyWithEntryId(string Key, string EntryId);

public class Xread : Base
{
    protected override string Name => nameof(Xread);
    public override bool CanBePropagated => false;
    private const string EntryIdPattern = @"^\d+-\d+$";
    private const string Block = "block";

    protected override async Task<string> ExecuteCore(CommandContext commandContext)
    {
        var blockIndex = FindCommandPartIndex(commandContext.CommandDetails.CommandParts, Block);
        
        if (blockIndex != -1 && int.TryParse(
                commandContext.CommandDetails.CommandParts[blockIndex + 2], out var blockTime))
        {
            await Task.Delay(blockTime);
        }

        if (blockIndex != -1 && commandContext.CommandDetails.CommandParts[blockIndex + 2] == "0")
        {
            return await ReadStreams(commandContext, noTimeout: true);
        }

        return await ReadStreams(commandContext);
    }

    private static Task<string> ReadStreams(CommandContext commandContext, bool noTimeout = false)
    {
        string resp;
        var isBlocking = FindCommandPartIndex(commandContext.CommandDetails.CommandParts, Block) != -1;
        List<StreamCacheItemValueItem> streamEntries = [];

        if (noTimeout)
        {
            if (commandContext.CommandDetails.CommandParts.Last() != "$")
            {
                while (!streamEntries.Any())
                {
                    var streamKeys = GetStreamKeysFromCommand(commandContext.CommandDetails, isBlocking);
                    if (streamKeys.Count == 0)
                    {
                        continue;
                    }

                    streamEntries.AddRange(BuildStreamEntries(streamKeys));
                }
            }
            else
            {
                var key = commandContext.CommandDetails.CommandParts[^3];

                string? maxEntryId = null;
                var maxExistingEntryId = DataCache.Fetch(key);
                if (!string.IsNullOrEmpty(maxExistingEntryId))
                {
                    var existingEntryId = maxExistingEntryId.Deserialize<StreamCacheItem>()!;
                    maxEntryId = existingEntryId.Value.Max(x => x.Id)!;
                }

                while (!streamEntries.Any())
                {
                    streamEntries.AddRange(BuildStreamEntries([new StreamKeyWithEntryId(key, maxEntryId ?? "0-0")]));
                }
            }
        }
        else
        {
            var streamKeys = GetStreamKeysFromCommand(commandContext.CommandDetails, isBlocking);
            if (streamKeys.Count == 0)
            {
                resp = RespBuilder.NullArray();
                commandContext.Socket.SendCommand(resp);

                return Task.FromResult(resp);
            }

            streamEntries.AddRange(BuildStreamEntries(streamKeys));
        }

        if (streamEntries.Count == 0)
        {
            resp = RespBuilder.NullArray();
            commandContext.Socket.SendCommand(resp);

            return Task.FromResult(resp);
        }

        resp = BuildResp(streamEntries);
        commandContext.Socket.SendCommand(resp);

        return Task.FromResult(resp);
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

            if (Regex.IsMatch(streamKey.EntryId, EntryIdPattern))
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

    private static List<StreamKeyWithEntryId> GetStreamKeysFromCommand(CommandDetails commandDetails, bool isBlocking)
    {
        var streamKeys = new List<string>();
        var streamKeysWithEntryIds = new List<StreamKeyWithEntryId>();

        if (!string.Equals(commandDetails.CommandParts[isBlocking ? 8 : 4], "streams",
                StringComparison.InvariantCultureIgnoreCase))
        {
            return streamKeysWithEntryIds;
        }

        streamKeys.AddRange(commandDetails.CommandParts.Skip(isBlocking ? 9 : 5)
            .Where(x => !Regex.IsMatch(x, @"^\$\d+$") && !Regex.IsMatch(x, EntryIdPattern)));

        var streamKeyEntryIds = new List<string>();
        var index = commandDetails.CommandParts.ToList().IndexOf(streamKeys.Last());
        for (var i = index + 1; i < commandDetails.CommandParts.Length; i++)
        {
            if (Regex.IsMatch(commandDetails.CommandParts[i], EntryIdPattern))
            {
                streamKeyEntryIds.Add(commandDetails.CommandParts[i]);
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

    private static int FindCommandPartIndex(IReadOnlyList<string> commandParts, string value)
    {
        for (var i = 0; i < commandParts.Count; i++)
        {
            if (string.Equals(commandParts[i], value, StringComparison.InvariantCultureIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }
    
    private static string BuildResp(List<StreamCacheItemValueItem> streamEntries)
    {
        var sb = new StringBuilder(RespBuilder.InitArray(streamEntries.Count));
        foreach (var streamEntry in streamEntries)
        {
            sb.Append(RespBuilder.InitArray(2));
            sb.Append(RespBuilder.BulkString(streamEntry.Key));
            sb.Append(RespBuilder.InitArray(1));
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
}
