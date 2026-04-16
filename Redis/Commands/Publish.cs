using System.Text;
using Redis.Cache;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Publish : Base
{
    protected override string Name => nameof(Publish);
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
        var channel = commands[4];
        var message = commands[6];
        
        var subscribers = DataCache.GetSubscriptions(channel);
        var resp = RespBuilder.Integer(subscribers);
        commandContext.Socket.SendCommand(resp);
        
        var sb = new StringBuilder(RespBuilder.InitArray(3));
        sb.Append(RespBuilder.BulkString(nameof(message).ToLower()));
        sb.Append(RespBuilder.BulkString(channel));
        sb.Append(RespBuilder.BulkString(message));
        
        DataCache.Publish(channel, sb.ToString());

        return Task.FromResult(resp);
    }
}
