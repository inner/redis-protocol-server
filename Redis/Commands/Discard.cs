using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Discard : Base
{
    protected override string Name => nameof(Discard);
    public override bool CanBePropagated => true;

    public override Dictionary<string, Dictionary<string, string>> Docs()
    {
        return new()
        {
            {
                Name,
                new()
                {
                    { "summary", "Discards a transaction." },
                    { "usage #1", "redis-cli" },
                    { "usage #2", "MULTI" },
                    { "usage #3", "SET mykey1 myval1" },
                    { "usage #4", "INCR someotherkey" },
                    { "usage #5", "DISCARD" }
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

    private Task<string> GenerateCommonResponse(CommandContext commandContext)
    {
        string result;

        if (commandContext.CommandQueue.Count > 0)
        {
            commandContext.CommandQueue.Clear();
            result = RespBuilder.SimpleString("OK");
        }
        else
        {
            result = RespBuilder.Error("DISCARD without MULTI");
        }

        commandContext.Socket.SendCommand(result);
        return Task.FromResult(result);
    }
}