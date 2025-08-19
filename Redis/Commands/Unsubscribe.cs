using System.Text;
using Redis.Cache;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Unsubscribe : Base
{
    protected override string Name => nameof(Unsubscribe);
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
        var channel = commandContext.CommandDetails.CommandParts[4];

        if (commandContext.Subscriptions.Contains(channel))
        {
            commandContext.Subscriptions.Remove(channel);
            DataCache.RemoveSubscription(channel, commandContext.Socket);
        }

        var sb = new StringBuilder(RespBuilder.InitArray(3));
        sb.Append(RespBuilder.BulkString(nameof(Unsubscribe).ToLower()));
        sb.Append(RespBuilder.BulkString(channel));
        sb.Append(RespBuilder.Integer(commandContext.Subscriptions.Count));
        var resp = sb.ToString();
        
        commandContext.Socket.SendCommand(resp);

        return Task.FromResult(resp);
    }
    
    public override Dictionary<string, Dictionary<string, string>> Docs()
    {
        return new()
        {
            {
                "Unsubscribe",
                new()
                {
                    { "description", "Unsubscribes from a channel." },
                    { "syntax", "UNSUBSCRIBE [channel]" },
                    { "group", "Pub/Sub" },
                    { "complexity", "O(N) where N is the number of subscriptions to the channel." }
                }
            }
        };
    }
}