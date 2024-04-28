using System.Net.Sockets;
using System.Text;

namespace codecrafters_redis.Commands;

public class Replconf : Base
{
    public override bool IsPropagated => true;
    
    public override void Execute(Socket socket, int commandCount, string[] commandParts)
    {
        if (string.Equals(commandParts[4], "listening-port", StringComparison.InvariantCultureIgnoreCase) ||
            string.Equals(commandParts[4], "capa", StringComparison.InvariantCultureIgnoreCase))
        {
            ServerInfo.ReplicaSockets.TryAdd(socket.RemoteEndPoint!.ToString()!, socket);
            socket.Send(Encoding.UTF8.GetBytes(Constants.OkResponse));
        }
        
        if (string.Equals(commandParts[4], "getack", StringComparison.InvariantCultureIgnoreCase) &&
            string.Equals(commandParts[6], "*", StringComparison.InvariantCultureIgnoreCase))
        {
            if (!ServerInfo.IsMaster)
            {
                socket.Send(Encoding.UTF8.GetBytes("*3\r\n$8\r\nREPLCONF\r\n$3\r\nACK\r\n$1\r\n0\r\n"));
            }
        }
    }
}