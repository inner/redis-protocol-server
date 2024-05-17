using System.Net.Sockets;
using System.Text;
using codecrafters_redis.Cache;

namespace codecrafters_redis.Commands;

public class Set : Base
{
    public override bool CanBePropagated => true;

    protected override Task OnMasterNodeExecute(Socket socket, int commandCount, string[] commandParts,
        bool replicaConnection = false)
    {
        var cacheKey = commandParts[4];
        var cacheValue = commandParts[6];

        if (commandParts.Length < 9)
        {
            DataCache.Set(cacheKey, cacheValue);
            socket.Send(Encoding.UTF8.GetBytes(Constants.OkResponse));
            return Task.CompletedTask;
        }

        const string expiryCommandConstant = "PX";

        var expiryCommand = commandParts[8];
        if (!string.Equals(expiryCommand, expiryCommandConstant, StringComparison.InvariantCultureIgnoreCase))
        {
            throw new AggregateException($"Unrecognized command used for '{nameof(Set)}': '{expiryCommand}'.");
        }

        var expiry = int.Parse(commandParts[10]);
        DataCache.Set(cacheKey, cacheValue, expiry);
        socket.Send(Encoding.UTF8.GetBytes(Constants.OkResponse));
        return Task.CompletedTask;
    }

    protected override Task OnReplicaNodeExecute(Socket socket, int commandCount, string[] commandParts,
        bool replicaConnection = false)
    {
        var cacheKey = commandParts[4];
        var cacheValue = commandParts[6];

        if (commandParts.Length < 9)
        {
            DataCache.Set(cacheKey, cacheValue);

            // if (!replicaConnection)
            // {
            //     socket.Send(Encoding.UTF8.GetBytes(Constants.OkArrayResponse));
            // }

            return Task.CompletedTask;
        }

        const string expiryCommandConstant = "PX";

        var expiryCommand = commandParts[8];
        if (!string.Equals(expiryCommand, expiryCommandConstant, StringComparison.InvariantCultureIgnoreCase))
        {
            throw new AggregateException($"Unrecognized command used for '{nameof(Set)}': '{expiryCommand}'.");
        }

        var expiry = int.Parse(commandParts[10]);
        DataCache.Set(cacheKey, cacheValue, expiry);

        // if (!replicaConnection)
        // {
        //     socket.Send(Encoding.UTF8.GetBytes(Constants.OkArrayResponse));
        // }
        
        return Task.CompletedTask;
    }
}