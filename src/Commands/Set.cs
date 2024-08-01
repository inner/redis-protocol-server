using System.Net.Sockets;
using System.Text;
using codecrafters_redis.Cache;
using codecrafters_redis.Common;
using codecrafters_redis.Receivers;

namespace codecrafters_redis.Commands;

public class Set : Base
{
    public override bool CanBePropagated => true;

    protected override Task OnMasterNodeExecute(Socket socket, CommandDetails commandDetails,
        List<CommandQueueItem> commandQueue, ReceiverBase receiver, bool replicaConnection = false)
    {
        var cacheKey = commandDetails.CommandParts[4];
        var cacheValue = commandDetails.CommandParts[6];

        if (commandDetails.CommandParts.Length < 9)
        {
            DataCache.Set(cacheKey, cacheValue);
            socket.Send(Encoding.UTF8.GetBytes(Constants.OkResponse));
            return Task.CompletedTask;
        }

        const string expiryCommandConstant = "PX";

        var expiryCommand = commandDetails.CommandParts[8];
        if (!string.Equals(expiryCommand, expiryCommandConstant, StringComparison.InvariantCultureIgnoreCase))
        {
            throw new Exception($"Unrecognized command used for '{nameof(Set)}': '{expiryCommand}'.");
        }

        var expiry = int.Parse(commandDetails.CommandParts[10]);
        DataCache.Set(cacheKey, cacheValue, DateTimeOffset.Now.AddMilliseconds(expiry).ToUnixTimeMilliseconds());
        
        socket.Send(Encoding.UTF8.GetBytes(Constants.OkResponse));
        
        return Task.CompletedTask;
    }

    protected override Task OnReplicaNodeExecute(Socket socket, CommandDetails commandDetails,
        List<CommandQueueItem> commandQueue, ReceiverBase receiver, bool replicaConnection = false)
    {
        var cacheKey = commandDetails.CommandParts[4];
        var cacheValue = commandDetails.CommandParts[6];

        if (commandDetails.CommandParts.Length < 9)
        {
            DataCache.Set(cacheKey, cacheValue);
            return Task.CompletedTask;
        }

        const string expiryCommandConstant = "PX";

        var expiryCommand = commandDetails.CommandParts[8];
        if (!string.Equals(expiryCommand, expiryCommandConstant, StringComparison.InvariantCultureIgnoreCase))
        {
            throw new AggregateException($"Unrecognized command used for '{nameof(Set)}': '{expiryCommand}'.");
        }

        var expiry = int.Parse(commandDetails.CommandParts[10]);
        DataCache.Set(cacheKey, cacheValue, DateTimeOffset.Now.AddMilliseconds(expiry).ToUnixTimeMilliseconds());
        
        return Task.CompletedTask;
    }
}