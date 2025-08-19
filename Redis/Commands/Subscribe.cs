using System.Text;
using Redis.Cache;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Subscribe : Base
{
    protected override string Name => nameof(Subscribe);
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
        var commands = commandContext.CommandDetails.CommandParts;
        var channel = commands[4];

        if (!commandContext.Subscriptions.Contains(channel))
        {
            commandContext.Subscriptions.Add(channel);
        }
        
        DataCache.AddSubscription(channel, commandContext.Socket);
        
        var sb = new StringBuilder(RespBuilder.InitArray(3));
        sb.Append(RespBuilder.BulkString(nameof(Subscribe).ToLower()));
        sb.Append(RespBuilder.BulkString(channel));
        sb.Append(RespBuilder.Integer(commandContext.Subscriptions.Count));
        
        var resp = sb.ToString();
        
        if (!commandContext.ReplicaConnection)
        {
            commandContext.Socket.SendCommand(resp);
        }
        
        // try
        // {
        //     while (commandContext.Socket.Connected)
        //     {
        //         using var memoryStream = new MemoryStream();
        //         var buffer = new byte[1024];
        //         int bytesRead;
        //         
        //         while ((bytesRead = commandContext.Socket.Receive(buffer)) > 0)
        //         {
        //             memoryStream.Write(buffer, 0, bytesRead);
        //             if (bytesRead < buffer.Length)
        //             {
        //                 break;
        //             }
        //         }
        //     }
        // }
        // catch (SocketException)
        // {
        //     // Handle socket exceptions, e.g., client disconnected
        //     // DataCache.RemoveSubscription(channel, commandContext.Socket);
        // }
        // catch (ObjectDisposedException)
        // {
        //     // Handle object disposed exceptions, e.g., socket closed
        //     // DataCache.RemoveSubscription(channel, commandContext.Socket);
        // }
        
        return Task.FromResult(resp);
    }

    public override Dictionary<string, Dictionary<string, string>> Docs()
    {
        return new()
        {
            {
                "Subscribe",
                new()
                {
                    { "description", "Subscribes to a channel to receive messages." },
                    { "syntax", "SUBSCRIBE channel" },
                    { "group", "Pub/Sub" },
                    { "complexity", "O(1) per message received." },
                    { "since", "1.0.0" }
                }
            }
        };
    }
}