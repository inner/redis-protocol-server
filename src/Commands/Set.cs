using System.Net.Sockets;
using System.Text;

namespace codecrafters_redis.Commands;

public class Set : Base
{
    public override bool CanBePropagated => true;

    protected override void OnMasterNodeExecute(Socket socket, int commandCount, string[] commandParts,
        bool replicaConnection = false)
    {
        var cacheKey = commandParts[4];
        var cacheValue = commandParts[6];

        if (commandParts.Length < 9)
        {
            DataCache.Set(cacheKey, cacheValue);

            if (ServerInfo.IsMaster)
            {
                socket.Send(Encoding.UTF8.GetBytes(Constants.OkResponse));
            }
            
            // if (!replicaConnection)
            // {
            //     socket.Send(Encoding.UTF8.GetBytes(Constants.OkArrayResponse));
            // }

            return;
        }

        const string expiryCommandConstant = "PX";

        var expiryCommand = commandParts[8];
        if (!string.Equals(expiryCommand, expiryCommandConstant, StringComparison.InvariantCultureIgnoreCase))
        {
            throw new AggregateException($"Unrecognized command used for '{nameof(Set)}': '{expiryCommand}'.");
        }

        var expiry = int.Parse(commandParts[10]);
        DataCache.Set(cacheKey, cacheValue, expiry);

        if (ServerInfo.IsMaster)
        {
            socket.Send(Encoding.UTF8.GetBytes(Constants.OkResponse));
        }
        
        // if (!replicaConnection)
        // {
        //     socket.Send(Encoding.UTF8.GetBytes(Constants.OkArrayResponse));
        // }
    }

    protected override void OnReplicaNodeExecute(Socket socket, int commandCount, string[] commandParts,
        bool replicaConnection = false)
    {
        var cacheKey = commandParts[4];
        var cacheValue = commandParts[6];

        if (commandParts.Length < 9)
        {
            DataCache.Set(cacheKey, cacheValue);

            if (ServerInfo.IsMaster)
            {
                socket.Send(Encoding.UTF8.GetBytes(Constants.OkResponse));
            }
            
            // if (!replicaConnection)
            // {
            //     socket.Send(Encoding.UTF8.GetBytes(Constants.OkArrayResponse));
            // }

            return;
        }

        const string expiryCommandConstant = "PX";

        var expiryCommand = commandParts[8];
        if (!string.Equals(expiryCommand, expiryCommandConstant, StringComparison.InvariantCultureIgnoreCase))
        {
            throw new AggregateException($"Unrecognized command used for '{nameof(Set)}': '{expiryCommand}'.");
        }

        var expiry = int.Parse(commandParts[10]);
        DataCache.Set(cacheKey, cacheValue, expiry);

        if (ServerInfo.IsMaster)
        {
            socket.Send(Encoding.UTF8.GetBytes(Constants.OkResponse));
        }
        
        // if (!replicaConnection)
        // {
        //     socket.Send(Encoding.UTF8.GetBytes(Constants.OkArrayResponse));
        // }
    }
}