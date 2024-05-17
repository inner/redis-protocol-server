using System.Net.Sockets;
using System.Text;
using codecrafters_redis.Cache;

namespace codecrafters_redis.Commands;

public class Type : Base
{
    public override bool CanBePropagated => false;

    protected override Task OnMasterNodeExecute(Socket socket, int commandCount, string[] commandParts,
        bool replicaConnection = false)
    {
        var key = commandParts[4];
        var fetchItem = DataCache.Fetch(key);

        if (fetchItem != null)
        {
            var basicCacheItem = fetchItem.Deserialize<BasicCacheItem>();
            if (basicCacheItem != null && string.Equals(basicCacheItem.Type, nameof(BasicCacheItem),
                    StringComparison.InvariantCultureIgnoreCase))
            {
                socket.Send(Encoding.UTF8.GetBytes("+string\r\n"));
                return Task.CompletedTask;
            }

            var streamCacheItem = fetchItem.Deserialize<StreamCacheItem>();
            if (streamCacheItem != null && string.Equals(streamCacheItem.Type, nameof(StreamCacheItem),
                    StringComparison.InvariantCultureIgnoreCase))
            {
                socket.Send(Encoding.UTF8.GetBytes("+stream\r\n"));
                return Task.CompletedTask;
            }
        }

        socket.Send(Encoding.UTF8.GetBytes("+none\r\n"));
        return Task.CompletedTask;
    }

    protected override Task OnReplicaNodeExecute(Socket socket, int commandCount, string[] commandParts,
        bool replicaConnection = false)
    {
        return Task.CompletedTask;
    }
}