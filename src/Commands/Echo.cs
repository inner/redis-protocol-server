using System.Net.Sockets;
using System.Text;

namespace codecrafters_redis.Commands;

public class Echo : Base
{
    public override bool IsPropagated => false;

    public override void Execute(Socket socket, int commandCount, string[] commandParts, bool replicaConnection = false)
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