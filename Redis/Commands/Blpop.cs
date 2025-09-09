using System.Text;
using Redis.Cache;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Blpop : Base
{
    protected override string Name => nameof(Blpop);
    public override bool CanBePropagated => true;

    protected override async Task<string> OnMasterNodeExecute(CommandContext commandContext)
    {
        return await GenerateCommonResponse(commandContext);
    }

    protected override async Task<string> OnReplicaNodeExecute(CommandContext commandContext)
    {
        return await GenerateCommonResponse(commandContext);
    }

    private static async Task<string> GenerateCommonResponse(CommandContext commandContext)
    {
        var commands = commandContext.CommandDetails.CommandParts;
        var key = commands[4];
        var timeout = commands.Length > 6
            ? double.Parse(commands[6])
            : 0.0;
        
        var result = await DataCache.Blpop(key, timeout);
        string resp;

        if (result.Length == 0)
        {
            resp = RespBuilder.NullArray();
            commandContext.Socket.SendCommand(resp);

            return resp;
        }

        var sb = new StringBuilder(
            RespBuilder.InitArray(result.Length));

        foreach (var item in result)
        {
            sb.Append(RespBuilder.SimpleString(item));
        }

        resp = sb.ToString();
        commandContext.Socket.SendCommand(resp);

        return resp;
    }

    public override Dictionary<string, Dictionary<string, string>> Docs()
    {
        return new Dictionary<string, Dictionary<string, string>>
        {
            {
                "Blpop", new Dictionary<string, string>
                {
                    {"description", "Removes and returns the first element of the list stored at key. If the list is empty, it blocks until an element is available or the timeout is reached."},
                    {"syntax", "BLPOP key [key ...] timeout"},
                    {"group", "List"},
                    {"complexity", "O(1) for each key"},
                    {"since", "1.0.0"}
                }
            }
        };
    }
}