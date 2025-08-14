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

    private static Task<string> GenerateCommonResponse(CommandContext commandContext)
    {
        var commands = commandContext.CommandDetails.CommandParts;
        var key = commands[4];
        var timeout = commands.Length > 5
            ? int.Parse(commands[5])
            : 0;
        
        var result = DataCache.Blpop(key, timeout);

        if (result.Length == 0)
        {
            return Task.FromResult(RespBuilder.Null());
        }

        var sb = new StringBuilder(
            RespBuilder.InitArray(result.Length));

        foreach (var item in result)
        {
            sb.Append(RespBuilder.SimpleString(item));
        }

        var resp = sb.ToString();

        if (!commandContext.ReplicaConnection)
        {
            commandContext.Socket.SendCommand(resp);
        }

        return Task.FromResult(resp);
    }

    public override Dictionary<string, Dictionary<string, string>> Docs()
    {
        throw new NotImplementedException();
    }
}