using System.Net.Sockets;
using System.Text;

namespace codecrafters_redis.Commands;

public class Set : Base
{
    public override void Execute(Socket socket, int commandCount, string[] commandParts)
    {
        var cacheKey = commandParts[4];
        var cacheValue = commandParts[6];
        
        if (commandParts.Length < 9)
        {
            DataCache.Set(cacheKey, cacheValue);
            socket.Send(Encoding.UTF8.GetBytes(Constants.OkResponse));
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
    }
}