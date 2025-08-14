using System.Text;
using Redis.Commands.Common;
using Redis.Common;
using Redis.Receivers;

namespace Redis.Commands;

public class Exec : Base
{
    protected override string Name => nameof(Exec);
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
        string result;

        if (commandContext.CommandQueue.All(x => x.RespType != RespType.Multi))
        {
            result = RespBuilder.Error("EXEC without MULTI");
            commandContext.Socket.SendCommand(result);
            return result;
        }

        if (commandContext.CommandQueue.Count == 1 &&
            commandContext.CommandQueue.Single().RespType == RespType.Multi)
        {
            result = RespBuilder.EmptyArray();
            commandContext.Socket.SendCommand(result);
            commandContext.CommandQueue.Clear();
            return result;
        }

        List<string> commandResults = [];

        foreach (var commandInQueue in commandContext.CommandQueue)
        {
            if (commandInQueue.RespType == RespType.Multi)
            {
                continue;
            }

            var commandDetails = commandInQueue.Resp.BuildCommandDetails();
            commandDetails.FromTransaction = true;
            
            commandResults.Add(
                await commandContext.Receiver
                    .ExecuteCommand(
                        commandContext.Socket, commandDetails, []));
        }

        var sb = new StringBuilder($"*{commandResults.Count}\r\n");
        foreach (var commandResult in commandResults)
        {
            sb.Append($"{commandResult}");
        }
        
        commandContext.Socket.SendCommand(sb.ToString());
        commandContext.CommandQueue.Clear();
        
        return string.Empty;
    }
    
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
                    { "usage #5", "EXEC" },
                    { "usage #6", "GET mykey1" }
                }
            }
        };
    }
}