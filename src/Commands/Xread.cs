using System.Text;
using System.Text.RegularExpressions;
using codecrafters_redis.Cache;
using codecrafters_redis.Commands.Common;
using codecrafters_redis.Common;

namespace codecrafters_redis.Commands;

public record StreamKeyWithEntryId(string Key, string EntryId);

public class Xread : Base
{
    public override bool CanBePropagated => false;

    protected override async Task<string> OnMasterNodeExecute(CommandContext commandContext)
    {
        var blockIndex = Array.IndexOf(commandContext.CommandDetails.CommandParts, "block") + 1;
        if (blockIndex != -1 &&
            int.TryParse(commandContext.CommandDetails.CommandParts[blockIndex + 1], out var blockTime))
        {
            await Task.Delay(blockTime);
        }

        if (blockIndex != -1 && (commandContext.CommandDetails.CommandParts[6] == "\\x00" ||
                                 commandContext.CommandDetails.CommandParts[6] == "0"))
        {
            return await GenerateCommonResponse(commandContext, noTimeout: true);
        }

        return await GenerateCommonResponse(commandContext);
    }

    protected override async Task<string> OnReplicaNodeExecute(CommandContext commandContext)
    {
        var blockIndex = Array.IndexOf(commandContext.CommandDetails.CommandParts, "block") + 1;

        if (blockIndex != -1 &&
            int.TryParse(commandContext.CommandDetails.CommandParts[blockIndex + 1], out var blockTime))
        {
            await Task.Delay(blockTime);
        }

        if (blockIndex != -1 && (commandContext.CommandDetails.CommandParts[6] == "\\x00" ||
                                 commandContext.CommandDetails.CommandParts[6] == "0"))
        {
            return await GenerateCommonResponse(commandContext, noTimeout: true);
        }

        return await GenerateCommonResponse(commandContext);
    }

    private static Task<string> GenerateCommonResponse(CommandContext commandContext, bool noTimeout = false)
    {
        string result;
        var isBlocking = Array.IndexOf(commandContext.CommandDetails.CommandParts, "block") != -1;
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
                result = RespBuilder.Null();

                if (!commandContext.ReplicaConnection)
                {
                    commandContext.Socket.Send(result.AsBytes());
                }

                return Task.FromResult(result);
            }

            streamEntries.AddRange(BuildStreamEntries(streamKeys));
        }

        if (streamEntries.Count == 0)
        {
            result = RespBuilder.Null();

            if (!commandContext.ReplicaConnection)
            {
                commandContext.Socket.Send(result.AsBytes());
            }

            return Task.FromResult(result);
        }

        var sb = new StringBuilder($"*{streamEntries.Count}\r\n");
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

        result = sb.ToString();

        if (!commandContext.ReplicaConnection)
        {
            commandContext.Socket.Send(result.AsBytes());
        }

        return Task.FromResult(result);
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
            .Where(x => !Regex.IsMatch(x, @"^\$\d+$") && !Regex.IsMatch(x, @"^\d+-\d+$")));

        var streamKeyEntryIds = new List<string>();
        var index = commandDetails.CommandParts.ToList().IndexOf(streamKeys.Last());
        for (var i = index + 1; i < commandDetails.CommandParts.Length; i++)
        {
            if (Regex.IsMatch(commandDetails.CommandParts[i], @"^\d+-\d+$"))
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
}