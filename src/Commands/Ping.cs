using System.Net.Sockets;
using System.Text;

namespace codecrafters_redis.Commands;

public class Ping : Base
{
    public override bool CanBePropagated => true;
    
    public override void Execute(Socket socket, int commandCount, string[] commandParts, int bytesReceived,
        bool replicaConnection = false)
    {
        // var response = commandCount switch
        // {
        //     1 => "+PONG\r\n",
        //     // 1 => "*1\r\n$4\r\nPONG\r\n",
        //     2 => $"${commandParts[4].Length}\r\n{commandParts[4]}\r\n",
        //     _ => throw new ArgumentException("Wrong number of arguments for 'ping' command")
        // };

        var response = ServerInfo.IsMaster
            ? "+PONG\r\n"
            : "*1\r\n$4\r\nPONG\r\n";

        if (ServerInfo.ReplicaHandshakeCompleted)
        {
            socket.Send(Encoding.UTF8.GetBytes(response));
        }
    }
}