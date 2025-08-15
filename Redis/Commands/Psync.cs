using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Psync : Base
{
    protected override string Name => nameof(Psync);
    public override bool CanBePropagated => false;

    protected override Task<string> OnMasterNodeExecute(CommandContext commandContext)
    {
        var fullResync = RespBuilder.SimpleString($"FULLRESYNC {ServerInfo.ServerRuntimeContext.MasterReplId} 0");
        commandContext.Socket.SendCommand(fullResync);

        var rdbFile = GetHardcodedEmptyRdbFile();
        var rdbResynchronizationFileMsg = $"${rdbFile.Length}\r\n".AsBytes()
            .Concat(rdbFile)
            .ToArray();

        commandContext.Socket.Send(rdbResynchronizationFileMsg);

        ServerInfo.ServerRuntimeContext.Replicas.TryAdd(
            commandContext.ClientId,
            commandContext.Socket);

        return Task.FromResult(string.Empty);
    }

    private static byte[] GetHardcodedEmptyRdbFile()
    {
        return Convert.FromBase64String(
            "UkVESVMwMDEx+glyZWRpcy12ZXIFNy4yLjD6CnJlZGlzLWJpdHPAQPoFY3Rpb" +
            "WXCbQi8ZfoIdXNlZC1tZW3CsMQQAPoIYW9mLWJhc2XAAP/wbjv+wP9aog==");
    }

    public override Dictionary<string, Dictionary<string, string>> Docs()
    {
        return new()
        {
            {
                Name,
                new()
                {
                    { "summary", "An internal command used in replication." }
                }
            }
        };
    }
}