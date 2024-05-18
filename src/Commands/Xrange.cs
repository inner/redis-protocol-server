using System.Net.Sockets;
using System.Text;
using codecrafters_redis.Cache;

namespace codecrafters_redis.Commands;

public class Xrange : Base
{
    public override bool CanBePropagated => false;

    protected override Task OnMasterNodeExecute(Socket socket, int commandCount, string[] commandParts,
        bool replicaConnection = false)
    {
        var key = commandParts[4];

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

        var startEntryId = commandParts[6];
        var endEntryId = commandParts[8];

        long? startTimestamp = null;
        long? startSequence = null;
        long? endTimestamp = null;
        long? endSequence = null;

        if (startEntryId.Contains("-"))
        {
            startTimestamp = long.Parse(startEntryId.Split('-')[0]);
            startSequence = long.Parse(startEntryId.Split('-')[1]);
        }

        if (endEntryId.Contains("-"))
        {
            endTimestamp = long.Parse(endEntryId.Split('-')[0]);
            endSequence = long.Parse(endEntryId.Split('-')[1]);
        }

        var streamEntries = streamCacheItem.Value
            .Where(x =>
            {
                if (startTimestamp.HasValue && startSequence.HasValue)
                {
                    if (x.IdTimestamp < startTimestamp.Value)
                    {
                        return false;
                    }

                    if (x.IdTimestamp == startTimestamp.Value && x.IdSequence < startSequence.Value)
                    {
                        return false;
                    }
                }

                if (endTimestamp.HasValue && endSequence.HasValue)
                {
                    if (x.IdTimestamp > endTimestamp.Value)
                    {
                        return false;
                    }

                    if (x.IdTimestamp == endTimestamp.Value && x.IdSequence > endSequence.Value)
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

        var str = sb.ToString();
        socket.Send(Encoding.UTF8.GetBytes(str));
        return Task.CompletedTask;
    }

    protected override Task OnReplicaNodeExecute(Socket socket, int commandCount, string[] commandParts,
        bool replicaConnection = false)
    {
        throw new NotImplementedException();
    }
}