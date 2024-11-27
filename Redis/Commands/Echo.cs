using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Echo : Base
{
    public override bool CanBePropagated => false;

    public override Dictionary<string, Dictionary<string, string>> Docs()
    {
        return new()
        {
            {
                "ECHO",
                new()
                {
                    {"summary", "Returns the given string."},
                    {"usage", "redis-cli ECHO mystring"}
                }
            }
        };
    }

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
        var response = commandContext.CommandDetails.CommandCount switch
        {
            2 => RespBuilder.BulkString(commandContext.CommandDetails.CommandParts[4]),
            _ => RespBuilder.Error("Wrong number of arguments")
        };

        if (!commandContext.ReplicaConnection)
        {
            commandContext.Socket.SendCommand(response);
        }

        return Task.FromResult(response);
    }
}