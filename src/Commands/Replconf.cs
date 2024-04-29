using System.Net.Sockets;
using System.Text;

namespace codecrafters_redis.Commands;

public class Replconf : Base
{
    public override bool IsPropagated => false;
    
    public override void Execute(Socket socket, int commandCount, string[] commandParts, bool replicaConnection = false)
    {
        if (string.Equals(commandParts[4], "listening-port", StringComparison.InvariantCultureIgnoreCase) ||
            string.Equals(commandParts[4], "capa", StringComparison.InvariantCultureIgnoreCase))
        {
            socket.Send(Encoding.UTF8.GetBytes(Constants.OkResponse));
        }
        
        if (string.Equals(commandParts[4], "getack", StringComparison.InvariantCultureIgnoreCase) &&
            string.Equals(commandParts[6], "*", StringComparison.InvariantCultureIgnoreCase))
        {
            socket.Send(Encoding.UTF8.GetBytes("*3\r\n$8\r\nREPLCONF\r\n$3\r\nACK\r\n$1\r\n0\r\n"));
            
            // var buffer = new byte[1024];
            // var bytesReceived = socket.Receive(buffer);
            // var response = Encoding.UTF8.GetString(buffer, 0, bytesReceived);
            // if (!response.Contains("ACK"))
            // {
            //     // Handle the case where a GETACK doesn’t receive an ACK back
            //     // This could be logging the error, retrying the command, etc.
            //     socket.Send(Encoding.UTF8.GetBytes(Constants.NullResponse));
            // }
        }
        
        if (string.Equals(commandParts[4], "ack", StringComparison.InvariantCultureIgnoreCase) &&
            string.Equals(commandParts[6], "0", StringComparison.InvariantCultureIgnoreCase))
        {
            socket.Send(Encoding.UTF8.GetBytes(Constants.NullResponse));
        }
    }
}