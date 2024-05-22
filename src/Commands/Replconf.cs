using System.Net.Sockets;
using System.Text;

namespace codecrafters_redis.Commands;

public class Replconf : Base
{
    public override bool CanBePropagated => false;

    protected override Task OnMasterNodeExecute(Socket socket, int commandCount, string[] commandParts,
        bool replicaConnection = false)
    {
        if (string.Equals(commandParts[4], "listening-port", StringComparison.InvariantCultureIgnoreCase) ||
            string.Equals(commandParts[4], "capa", StringComparison.InvariantCultureIgnoreCase))
        {
            socket.Send(Encoding.UTF8.GetBytes(Constants.OkResponse));
        }
        
        if (string.Equals(commandParts[4], "ack", StringComparison.InvariantCultureIgnoreCase))
        {
            ServerInfo.Replication.IncrementReplicaAcksReceived();
            Console.WriteLine($"Received ACK from replica '{socket.RemoteEndPoint}', bytes received: {commandParts[6]}.");
            Console.WriteLine($"Replica ACKs received: {ServerInfo.Replication.ReplicaAcksReceived}.");
        }
        
        return Task.CompletedTask;
    }

    protected override Task OnReplicaNodeExecute(Socket socket, int commandCount, string[] commandParts,
        bool replicaConnection = false)
    {
        if (!ServerInfo.Replication.ReplicaHandshakeCompleted)
        {
            return Task.CompletedTask;
        }
        
        if (string.Equals(commandParts[4], "getack",
                StringComparison.InvariantCultureIgnoreCase) &&
            string.Equals(commandParts[6], "*", StringComparison.InvariantCultureIgnoreCase))
        {
            socket.Send(Encoding.UTF8.GetBytes($"*3\r\n$8\r\nREPLCONF\r\n$3\r\nACK\r\n${ServerInfo.Replication.ReplicaBytesReceived.ToString().Length}\r\n{ServerInfo.Replication.ReplicaBytesReceived}\r\n"));
        }
        
        return Task.CompletedTask;
    }
}