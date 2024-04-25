using System.Net.Sockets;
using System.Text;
using codecrafters_redis.Network;

namespace codecrafters_redis.Commands;

public class Replconf : Base
{
    public override bool IsPropagated => false;
    
    public override void Execute(Socket socket, int commandCount, string[] commandParts)
    {
        SlaveServers.SlaveSockets.TryAdd(socket.RemoteEndPoint!.ToString()!, socket);
        socket.Send(Encoding.UTF8.GetBytes(Constants.OkResponse));
    }
}