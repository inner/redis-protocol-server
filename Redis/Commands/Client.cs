using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Client : Base
{
    protected override string Name => nameof(Client);
    public override bool CanBePropagated => false;

    protected override async Task<string> OnMasterNodeExecute(CommandContext commandContext)
    {
        return await GenerateCommonResponse(commandContext);
    }

    protected override async Task<string> OnReplicaNodeExecute(CommandContext commandContext)
    {
        return await GenerateCommonResponse(commandContext);
    }

    private static Task<string> GenerateCommonResponse(CommandContext commandContext)
    {
        var resp = RespBuilder.SimpleString("OK");
        commandContext.Socket.SendCommand(resp);

        return Task.FromResult(resp);
    }
    
    public override Dictionary<string, Dictionary<string, string>> Docs()
    {
        return new()
        {
            {
                Name,
                new()
                {
                    { "summary", "A container for client connection commands." }
                }
            }
        };
    }
}