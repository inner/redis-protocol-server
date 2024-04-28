using System.Net.Sockets;
using System.Text;

namespace codecrafters_redis.Commands;

public class Get : Base
{
    public override bool IsPropagated => false;

    public override void Execute(Socket socket, int commandCount, string[] commandParts)
    {
        var cacheKey = commandParts[4];
        var cacheItem = DataCache.Get(cacheKey);

        var response = cacheItem is null or { Value: null }
            ? Constants.NullResponse
            : $"${cacheItem.Value.Length}\r\n{cacheItem.Value}\r\n";

        if (ServerInfo.IsMaster)
        {
            socket.Send(Encoding.UTF8.GetBytes(response));
        }
    }
}