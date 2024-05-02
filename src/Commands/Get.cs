using System.Net.Sockets;
using System.Text;

namespace codecrafters_redis.Commands;

public class Get : Base
{
    public override bool CanBePropagated => false;

    protected override void OnMasterNodeExecute(Socket socket, int commandCount, string[] commandParts, int bytesReceived,
        bool replicaConnection = false)
    {
        GenerateCommonResponse(socket, commandParts, replicaConnection);
    }

    protected override void OnReplicaNodeExecute(Socket socket, int commandCount, string[] commandParts, int bytesReceived,
        bool replicaConnection = false)
    {
        GenerateCommonResponse(socket, commandParts, replicaConnection);
    }

    private static void GenerateCommonResponse(Socket socket, string[] commandParts, bool replicaConnection)
    {
        var cacheKey = commandParts[4];
        var cacheItem = DataCache.Get(cacheKey);

        var response = cacheItem is null or { Value: null }
            ? Constants.NullResponse
            : $"${cacheItem.Value.Length}\r\n{cacheItem.Value}\r\n";

        if (!replicaConnection)
        {
            socket.Send(Encoding.UTF8.GetBytes(response));
        }
    }
}