using codecrafters_redis.Commands.Common;
using codecrafters_redis.Common;

namespace codecrafters_redis.Commands;

public class Psync : Base
{
    public override bool CanBePropagated => false;

    protected override Task<string> OnMasterNodeExecute(CommandContext commandContext)
    {
        var fullResync = RespBuilder.SimpleString($"FULLRESYNC {ServerInfo.ServerRuntimeContext.MasterReplId} 0");
        commandContext.Socket.Send(fullResync.AsBytes());
        
        var rdbFile = GetHardcodedEmptyRdbFile();
        var rdbResynchronizationFileMsg = $"${rdbFile.Length}\r\n".AsBytes()
            .Concat(rdbFile)
            .ToArray();
        
        commandContext.Socket.Send(rdbResynchronizationFileMsg);
        
        ServerInfo.ServerRuntimeContext.Replicas.TryAdd(
            commandContext.Socket.RemoteEndPoint!.ToString()!,
            commandContext.Socket);
        
        return Task.FromResult(string.Empty);
    }
    
    private static byte[] GetHardcodedEmptyRdbFile()
    {
        return Convert.FromBase64String(
            "UkVESVMwMDEx+glyZWRpcy12ZXIFNy4yLjD6CnJlZGlzLWJpdHPAQPoFY3Rpb" +
            "WXCbQi8ZfoIdXNlZC1tZW3CsMQQAPoIYW9mLWJhc2XAAP/wbjv+wP9aog==");
    }
}