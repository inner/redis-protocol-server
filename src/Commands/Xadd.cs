using System.Collections.Specialized;
using System.Net.Sockets;
using System.Text;

namespace codecrafters_redis.Commands;

public class Xadd : Base
{
    public override bool CanBePropagated => true;

    protected override Task OnMasterNodeExecute(Socket socket, int commandCount, string[] commandParts,
        bool replicaConnection = false)
    {
        var key = commandParts[4];
        var entryId = commandParts[6];

        Dictionary<string, string> value;

        try
        {
            value = BuildEntryValue(key, entryId, commandParts);
        }
        catch (Exception ex)
        {
            socket.Send(Encoding.UTF8.GetBytes($"-ERR {ex.Message}\r\n"));
            return Task.CompletedTask;
        }

        var newOrExistingEntryId = DataCache.Xadd(key, value);
        socket.Send(Encoding.UTF8.GetBytes($"+{newOrExistingEntryId}\r\n"));

        return Task.CompletedTask;
    }

    private Dictionary<string, string> BuildEntryValue(string key, string entryId, string[] commandParts)
    {
        // Explicit ("1526919030474-0") (This stage)
        // Auto-generate only sequence number ("1526919030474-*") (Next stages)
        // Auto-generate time part and sequence number ("*") (Next stages)
        
        // In C#, if you need to maintain the order of items as they are added,
        // you can use the OrderedDictionary class from the System.Collections.Specialized namespace.
        // However, this class is non-generic. If you want a generic version, you can use
        // Dictionary<TKey, TValue> in combination with List<T> to maintain the order of items.

        // var od = new OrderedDictionary
        // {
        //     { "Id", entryId }
        // };

        if (!string.Equals(entryId, "*") && !entryId.Contains('-'))
        {
            throw new Exception("wrong ID argument for 'XADD' command");
        }

        string? entryIdTimestampValue = null;
        string? entryIdSequenceValue = null;

        if (entryId.Contains('-'))
        {
            entryIdTimestampValue = entryId.Split('-')[0];
            entryIdSequenceValue = entryId.Split('-')[1];
        }

        Dictionary<string, string> value = new();
        const string idConst = "Id";

        var fetchItem = DataCache.Fetch(key);
        string? existingEntryId = null;

        if (!string.IsNullOrEmpty(fetchItem))
        {
            existingEntryId = fetchItem.Deserialize<StreamCacheItem>()!.Value
                .Last(x => x.Key == idConst).Value;
        }

        if (string.Equals(existingEntryId, entryId, StringComparison.InvariantCultureIgnoreCase))
        {
            throw new Exception("the ID specified in XADD is equal to the last ID in the stream.");
        }

        value.Add(idConst, entryId);

        for (var i = 8; i < commandParts.Length; i += 2)
        {
            var valueIndex = i + 2;
            value[commandParts[i]] = commandParts[valueIndex];
            i += 2;
            if (valueIndex + 2 >= commandParts.Length - 1)
            {
                break;
            }
        }

        return value;
    }

    protected override Task OnReplicaNodeExecute(Socket socket, int commandCount, string[] commandParts,
        bool replicaConnection = false)
    {
        throw new NotImplementedException();
    }
}