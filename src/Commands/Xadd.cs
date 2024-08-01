using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using codecrafters_redis.Cache;
using codecrafters_redis.Common;
using codecrafters_redis.Receivers;

namespace codecrafters_redis.Commands;

public class Xadd : Base
{
    public override bool CanBePropagated => true;

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

    private Task GenerateCommonResponse(Socket socket, CommandDetails commandDetails, bool replicaConnection = false)
    {
        var key = commandDetails.CommandParts[4];
        var entryId = commandDetails.CommandParts[6];

        try
        {
            var values = BuildEntryValue(key, entryId, commandDetails);
            var newOrExistingEntryId = DataCache.Xadd(key, values);
            
            if (!replicaConnection)
            {
                socket.Send(Encoding.UTF8.GetBytes($"${newOrExistingEntryId.Length}\r\n{newOrExistingEntryId}\r\n"));
            }

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            socket.Send(Encoding.UTF8.GetBytes($"-ERR {ex.Message}\r\n"));
            return Task.CompletedTask;
        }
    }

    private StreamCacheItemValueItem BuildEntryValue(string key, string entryId, CommandDetails commandDetails)
    {
        var entryIdType = GetEntryIdType(entryId);

        string? existingEntryId = null;

        var fetchStreamCacheItem = DataCache.Fetch(key);
        if (!string.IsNullOrEmpty(fetchStreamCacheItem))
        {
            var existingStreamCacheItem = fetchStreamCacheItem.Deserialize<StreamCacheItem>();
            if (existingStreamCacheItem != null)
            {
                existingEntryId = existingStreamCacheItem.Value
                    .Last().Id;

                if (string.Equals(existingEntryId, entryId, StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new Exception("The ID specified in XADD is equal or smaller than the target stream top item");
                }
            }
        }

        var value = entryIdType switch
        {
            EntryIdType.Preset => GetEntryIdValueForPreset(entryId, existingEntryId),
            EntryIdType.AutoSequence => GetEntryIdValueForAutoSequence(entryId, existingEntryId),
            EntryIdType.Auto => GetEntryIdValueForAuto(existingEntryId),
            _ => throw new Exception("invalid stream ID specified")
        };

        var values = new List<StreamCacheItemValueItemValue>();
        for (var i = 8; i < commandDetails.CommandParts.Length; i += 2)
        {
            var valueIndex = commandDetails.CommandParts[i].Contains(' ') ? i : i + 2;
            values.Add(new StreamCacheItemValueItemValue
            {
                Key = commandDetails.CommandParts[i],
                Value = commandDetails.CommandParts[valueIndex]
            });
            value.Key = key;
            value.Value = values;
            i += 2;
            if (valueIndex + 2 >= commandDetails.CommandParts.Length - 1)
            {
                break;
            }
        }

        return value;
    }

    private StreamCacheItemValueItem GetEntryIdValueForPreset(string entryId, string? existingEntryId)
    {
        StreamCacheItemValueItem value = new();

        var entryIdTimestamp = long.Parse(entryId.Split('-')[0]);
        var entryIdSequence = long.Parse(entryId.Split('-')[1]);
        long? existingLastEntryIdTimestamp = null;
        long? existingLastEntryIdSequence = null;

        if (existingEntryId != null)
        {
            existingLastEntryIdTimestamp = long.Parse(existingEntryId.Split('-')[0]);
            existingLastEntryIdSequence = long.Parse(existingEntryId.Split('-')[1]);
        }

        if (existingEntryId != null)
        {
            if (entryIdTimestamp < existingLastEntryIdTimestamp ||
                (entryIdTimestamp == existingLastEntryIdTimestamp &&
                 entryIdSequence < existingLastEntryIdSequence))
            {
                throw new Exception("The ID specified in XADD is equal or smaller than the target stream top item");
            }

            value.Id = entryId;
        }
        else if (existingEntryId == null)
        {
            value.Id = entryId;
        }

        return value;
    }

    private StreamCacheItemValueItem GetEntryIdValueForAutoSequence(string entryId, string? existingEntryId)
    {
        StreamCacheItemValueItem value = new();

        var entryIdTimestamp = long.Parse(entryId.Split('-')[0]);
        long? existingLastEntryIdTimestamp = existingEntryId != null
            ? long.Parse(existingEntryId.Split('-')[0])
            : null;

        long? existingLastEntryIdSequence = existingEntryId != null
            ? long.Parse(existingEntryId.Split('-')[1])
            : null;

        if (existingEntryId != null)
        {
            if (entryIdTimestamp < existingLastEntryIdTimestamp)
            {
                throw new Exception("The ID specified in XADD is equal or smaller than the target stream top item");
            }

            var newSequence = existingLastEntryIdTimestamp < entryIdTimestamp
                ? 0
                : existingLastEntryIdSequence + 1;

            value.Id = $"{entryIdTimestamp}-{newSequence}";
        }
        else if (existingEntryId == null)
        {
            value.Id = $"{entryIdTimestamp}-1";
        }

        return value;
    }

    private StreamCacheItemValueItem GetEntryIdValueForAuto(string? existingEntryId)
    {
        StreamCacheItemValueItem value = new();

        long? existingLastEntryIdTimestamp = existingEntryId != null
            ? long.Parse(existingEntryId.Split('-')[0])
            : null;

        long? existingLastEntryIdSequence = existingEntryId != null
            ? long.Parse(existingEntryId.Split('-')[1])
            : null;

        if (existingEntryId != null)
        {
            var newTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            newTimestamp = (long)(newTimestamp < existingLastEntryIdTimestamp
                ? existingLastEntryIdTimestamp + 1
                : newTimestamp);

            var newSequence = existingLastEntryIdSequence + 1;

            value.Id = $"{newTimestamp}-{newSequence}";
        }
        else if (existingEntryId == null)
        {
            var newTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            value.Id = $"{newTimestamp}-0";
        }

        return value;
    }

    private static EntryIdType GetEntryIdType(string entryId)
    {
        EntryIdType entryIdType;

        if (Regex.IsMatch(entryId, @"^\d+-\d+$"))
        {
            InitialPresetEntryIdValidation(entryId);
            entryIdType = EntryIdType.Preset;
        }
        else if (Regex.IsMatch(entryId, @"^\d+-\*$"))
        {
            InitialAutoSequenceEntryIdValidation(entryId);
            entryIdType = EntryIdType.AutoSequence;
        }
        else if (entryId == "*")
        {
            entryIdType = EntryIdType.Auto;
        }
        else
        {
            throw new Exception("invalid stream ID specified");
        }

        return entryIdType;
    }

    private static void InitialPresetEntryIdValidation(string entryId)
    {
        var errorMessage = "The ID specified in XADD must be greater than 0-0";

        var entryIdParts = entryId.Split('-');

        switch (long.Parse(entryIdParts[0]))
        {
            case < 0 when long.Parse(entryIdParts[1]) < 0:
                throw new Exception(errorMessage);
            case 0 when long.Parse(entryIdParts[1]) < 1:
                throw new Exception(errorMessage);
        }
    }

    private static void InitialAutoSequenceEntryIdValidation(string entryId)
    {
        var errorMessage = "The ID specified in XADD must be greater than 0-0";

        var entryIdParts = entryId.Split('-');

        switch (long.Parse(entryIdParts[0]))
        {
            case < 0:
                throw new Exception(errorMessage);
        }
    }
}