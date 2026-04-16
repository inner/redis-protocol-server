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
            sb.Append(RespBuilder.BulkString(item));
        }

        resp = sb.ToString();
        commandContext.Socket.SendCommand(resp);

        return resp;
    }
}
