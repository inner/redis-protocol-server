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
        
        var subscribers = DataCache.GetSubscriptionCount(channel);
        var resp = RespBuilder.Integer(subscribers);
        commandContext.Socket.SendCommand(resp);
        
        var sb = new StringBuilder(RespBuilder.InitArray(3));
        sb.Append(RespBuilder.BulkString("message"));
        sb.Append(RespBuilder.BulkString(channel));
        sb.Append(RespBuilder.BulkString(message));
        
        DataCache.SendToSubscribers(channel, sb.ToString());

        return Task.FromResult(resp);
    }

    public override Dictionary<string, Dictionary<string, string>> Docs()
    {
        return new()
        {
            {
                "Publish",
                new()
                {
                    { "description", "Publishes a message to a channel." },
                    { "syntax", "PUBLISH channel message" },
                    { "group", "Pub/Sub" },
                    { "complexity", "O(N) where N is the number of subscribers to the channel." }
                }
            }
        };
    }
}