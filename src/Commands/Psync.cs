using System.Net.Sockets;
using System.Text;

namespace codecrafters_redis.Commands;

public class Psync : Base
{
    public override bool CanBePropagated => false;

    protected override async Task OnMasterNodeExecute(Socket socket, int commandCount, string[] commandParts,
        bool replicaConnection = false)
    {
        var fullResyncResponse = $"+FULLRESYNC {ServerInfo.ServerRuntimeContext.MasterReplId} 0\r\n";
        
        var emptyRdbFileBase64 = "UkVESVMwMDEx+glyZWRpcy12ZXIFNy4yLjD6CnJlZGlzLWJpdHPAQPoFY3RpbWXCbQi8ZfoIdXNlZC1tZW3CsMQQAPoIYW9mLWJhc2XAAP/wbjv+wP9aog==";
        var rdbFile = Convert.FromBase64String(emptyRdbFileBase64);
        var rdbResynchronizationFileMsg = Encoding.UTF8.GetBytes($"${rdbFile.Length}\r\n")
            .Concat(rdbFile)
            .ToArray();
        
        socket.Send(Encoding.UTF8.GetBytes(fullResyncResponse));
        socket.Send(rdbResynchronizationFileMsg);
        
        await Task.Delay(500);
        ServerInfo.ServerRuntimeContext.Replicas.TryAdd(socket.RemoteEndPoint!.ToString()!, socket);
    }

    protected override Task OnReplicaNodeExecute(Socket socket, int commandCount, string[] commandParts,
        bool replicaConnection = false)
    {
        return Task.CompletedTask;
    }
}