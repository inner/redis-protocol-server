using Redis.Cache;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Lpop : Base
{
    protected override string Name => nameof(Lpop);
    public override bool CanBePropagated => true;

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
        var commands = commandContext.CommandDetails.CommandParts;

        var key = commands[4];

        var result = DataCache.Lpop(key);

        if (string.IsNullOrEmpty(result))
        {
            return Task.FromResult(RespBuilder.Null());
        }

        var resp = RespBuilder.SimpleString(result);

        if (!commandContext.ReplicaConnection)
        {
            commandContext.Socket.SendCommand(resp);
        }

        return Task.FromResult(resp);
    }

    public override Dictionary<string, Dictionary<string, string>> Docs()
    {
        return new()
        {
            {
                Name,
                new Dictionary<string, string>
                {
                    { "description", "Removes and returns the first element of the list stored at key." },
                    { "syntax", "LPOP key" }
                }
            }
        };
    }
}