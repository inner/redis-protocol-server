using System.Net.Sockets;
using System.Text;
using codecrafters_redis.Rdb;

namespace codecrafters_redis.Commands;

public class Keys : Base
{
    public override bool CanBePropagated => false;

    protected override Task OnMasterNodeExecute(Socket socket, int commandCount, string[] commandParts,
        bool replicaConnection = false)
    {
        var rdbReader = new RdbReader();
        var keys = rdbReader.ReadRdb(Path.Combine(ServerInfo.ServerRuntimeContext.DataDir,
            ServerInfo.ServerRuntimeContext.DbFilename));

        Console.WriteLine("Keys in the RDB file:");
        foreach (var key in keys)
        {
            socket.Send(Encoding.UTF8.GetBytes($"*1\r\n${key.Length}\r\n{key}\r\n"));
        }
        
        return Task.CompletedTask;
    }

    protected override Task OnReplicaNodeExecute(Socket socket, int commandCount, string[] commandParts,
        bool replicaConnection = false)
    {
        throw new NotImplementedException();
    }
}