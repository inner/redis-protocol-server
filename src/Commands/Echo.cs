using System.Net.Sockets;
using System.Text;

namespace codecrafters_redis.Commands;

public class Echo : Base
{
    public override bool CanBePropagated => false;

    protected override void OnMasterNodeExecute(Socket socket, int commandCount, string[] commandParts, int bytesReceived,
        bool replicaConnection = false)
    {
        GenerateCommonResponse(socket, commandCount, commandParts, replicaConnection);
    }

    protected override void OnReplicaNodeExecute(Socket socket, int commandCount, string[] commandParts, int bytesReceived,
        bool replicaConnection = false)
    {
        GenerateCommonResponse(socket, commandCount, commandParts, replicaConnection);
    }

    private static void GenerateCommonResponse(Socket socket, int commandCount, string[] commandParts, bool replicaConnection)
    {
        var response = commandCount switch
        {
            2 => $"${commandParts[4].Length}\r\n{commandParts[4]}\r\n",
            _ => throw new ArgumentException($"Wrong number of arguments for '{nameof(Echo)}' command: {commandCount}.")
        };

        if (!replicaConnection)
        {
            socket.Send(Encoding.UTF8.GetBytes(response));
        }
    }
}