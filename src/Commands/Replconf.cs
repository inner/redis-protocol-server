using System.Net.Sockets;
using System.Text;

namespace codecrafters_redis.Commands;

public class Replconf : Base
{
    public override bool CanBePropagated => false;

    public override void Execute(Socket socket, int commandCount, string[] commandParts, int bytesReceived,
        bool replicaConnection = false)
    {
        if (string.Equals(commandParts[4], "listening-port", StringComparison.InvariantCultureIgnoreCase) ||
            string.Equals(commandParts[4], "capa", StringComparison.InvariantCultureIgnoreCase))
        {
            socket.Send(Encoding.UTF8.GetBytes(Constants.OkResponse));
            return;
        }

        if (ServerInfo.ReplicaHandshakeCompleted && replicaConnection && string.Equals(commandParts[4], "getack",
                StringComparison.InvariantCultureIgnoreCase) &&
            string.Equals(commandParts[6], "*", StringComparison.InvariantCultureIgnoreCase))
        {
            var currentBytesRead = ServerInfo.ReplicaHandshakeCompleted
                ? ServerInfo.BytesRead - bytesReceived
                : 0;
            
            socket.Send(Encoding.UTF8.GetBytes($"*3\r\n$8\r\nREPLCONF\r\n$3\r\nACK\r\n${currentBytesRead.ToString().Length}\r\n{currentBytesRead}\r\n"));
            return;
        }

        if (replicaConnection && string.Equals(commandParts[4], "ack", StringComparison.InvariantCultureIgnoreCase) &&
            string.Equals(commandParts[6], "0", StringComparison.InvariantCultureIgnoreCase))
        {
            // socket.Send(Encoding.UTF8.GetBytes(Constants.NullResponse));
        }
    }
}