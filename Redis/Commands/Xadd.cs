using System.Text.RegularExpressions;
using Redis.Cache;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Xadd : Base
{
    protected override string Name => nameof(Xadd);
    public override bool CanBePropagated => true;

    protected override async Task<string> OnMasterNodeExecute(CommandContext commandContext)
    {
        return await GenerateCommonResponse(commandContext);
    }

    protected override async Task<string> OnReplicaNodeExecute(CommandContext commandContext)
    {
        return await GenerateCommonResponse(commandContext);
    }

    private Task<string> GenerateCommonResponse(CommandContext commandContext)
    {
        string result;
        var key = commandContext.CommandDetails.CommandParts[4];
        var entryId = commandContext.CommandDetails.CommandParts[6];

        try
        {
            var values = BuildEntryValue(key, entryId, commandContext.CommandDetails);
            var newOrExistingEntryId = DataCache.Xadd(key, values);

            result = RespBuilder.BulkString(newOrExistingEntryId);

            if (!commandContext.ReplicaConnection)
            {
                commandContext.Socket.SendCommand(result);
            }

            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            result = RespBuilder.Error(ex.Message);
            commandContext.Socket.SendCommand(result);
        }

        return Task.FromResult(result);
    }

    private StreamCacheItemValueItem BuildEntryValue(string key, string entryId, CommandDetails commandDetails)
    {
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

        var value = GetEntryIdType(entryId) switch
        {
            EntryIdType.Preset => GetEntryIdValueForPreset(entryId, existingEntryId),
            EntryIdType.AutoSequence => GetEntryIdValueForAutoSequence(entryId, existingEntryId),
            EntryIdType.Auto => GetEntryIdValueForAuto(existingEntryId),
            _ => throw new Exception("invalid stream ID specified")
        };

        var values = new List<StreamCacheItemValueItemValue>();
        
        for (var i = 8; i < commandDetails.CommandParts.Length; i += 2)
        {
            var valueIndex = commandDetails.CommandParts[i].Contains(' ')
                ? i
                : i + 2;

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

    private static StreamCacheItemValueItem GetEntryIdValueForAuto(string? existingEntryId)
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
        var entryIdParts = entryId.Split('-');
        var errorMessage = "The ID specified in XADD must be greater than 0-0";

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
        var entryIdParts = entryId.Split('-');
        var errorMessage = "The ID specified in XADD must be greater than 0-0";

        switch (long.Parse(entryIdParts[0]))
        {
            case < 0:
                throw new Exception(errorMessage);
        }
    }
}