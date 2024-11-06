using System.Text;

namespace codecrafters_redis.Commands;

public class Psync : Base
{
    public override bool CanBePropagated => false;

    protected override Task<string> OnMasterNodeExecute(CommandContext commandContext)
    {
        var fullResyncResponse = $"+FULLRESYNC {ServerInfo.ServerRuntimeContext.MasterReplId} 0\r\n";
        
        var emptyRdbFileBase64 = "UkVESVMwMDEx+glyZWRpcy12ZXIFNy4yLjD6CnJlZGlzLWJpdHPAQPoFY3RpbWXCbQi8ZfoIdXNlZC1tZW3CsMQQAPoIYW9mLWJhc2XAAP/wbjv+wP9aog==";
        var rdbFile = Convert.FromBase64String(emptyRdbFileBase64);
        var rdbResynchronizationFileMsg = Encoding.UTF8.GetBytes($"${rdbFile.Length}\r\n")
            .Concat(rdbFile)
            .ToArray();
        
        commandContext.Socket.Send(Encoding.UTF8.GetBytes(fullResyncResponse));
        commandContext.Socket.Send(rdbResynchronizationFileMsg);
        
        ServerInfo.ServerRuntimeContext.Replicas.TryAdd(
            commandContext.Socket.RemoteEndPoint!.ToString()!,
            commandContext.Socket);
        
        return Task.FromResult(string.Empty);
    }

    protected override Task<string> OnReplicaNodeExecute(CommandContext commandContext)
    {
        return Task.FromResult(string.Empty);
    }
}