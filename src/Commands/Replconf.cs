using System.Net.Sockets;
using System.Text;

namespace codecrafters_redis.Commands;

public class Replconf : Base
{
    public override bool CanBePropagated => true;

    protected override void OnMasterNodeExecute(Socket socket, int commandCount, string[] commandParts,
        bool replicaConnection = false)
    {
        if (string.Equals(commandParts[4], "listening-port", StringComparison.InvariantCultureIgnoreCase) ||
            string.Equals(commandParts[4], "capa", StringComparison.InvariantCultureIgnoreCase))
        {
            socket.Send(Encoding.UTF8.GetBytes(Constants.OkResponse));
        }
    }

    protected override void OnReplicaNodeExecute(Socket socket, int commandCount, string[] commandParts,
        bool replicaConnection = false)
    {
        if (!ServerInfo.ReplicaHandshakeCompleted)
        {
            return;
        }
        
        if (/* replicaConnection && */ string.Equals(commandParts[4], "getack",
                StringComparison.InvariantCultureIgnoreCase) &&
            string.Equals(commandParts[6], "*", StringComparison.InvariantCultureIgnoreCase))
        {
            socket.Send(Encoding.UTF8.GetBytes($"*3\r\n$8\r\nREPLCONF\r\n$3\r\nACK\r\n${ServerInfo.BytesReceived.ToString().Length}\r\n{ServerInfo.BytesReceived}\r\n"));
        }
    }
}