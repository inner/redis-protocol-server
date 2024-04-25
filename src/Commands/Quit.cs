using System.Net.Sockets;
using System.Text;

namespace codecrafters_redis.Commands;

public class Quit : Base
{
    public override bool IsPropagated => false;
    
    public override void Execute(Socket socket, int commandCount, string[] commandParts)
    {
        socket.Send(Encoding.UTF8.GetBytes(Constants.OkResponse));
    }
}