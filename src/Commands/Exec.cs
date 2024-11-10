using System.Text;
using codecrafters_redis.Common;

namespace codecrafters_redis.Commands;

public class Exec : Base
{
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

        if (commandContext.CommandQueue.All(x => x.CommandType != CommandType.Multi))
        {
            result = RespBuilder.BuildRespError("EXEC without MULTI");
            commandContext.Socket.Send(result.AsBytes());
            return result;
        }

        if (commandContext.CommandQueue.Count == 1 &&
            commandContext.CommandQueue.Single().CommandType == CommandType.Multi)
        {
            result = RespBuilder.BuildRespArray();
            commandContext.Socket.Send(result.AsBytes());
            commandContext.CommandQueue.Clear();
            return result;
        }

        List<string> commandResults = [];

        foreach (var commandInQueue in commandContext.CommandQueue)
        {
            if (commandInQueue.CommandType == CommandType.Multi)
            {
                continue;
            }

            var commandDetails = commandInQueue.CommandString
                .Replace("\r\n", @"\r\n")
                .BuildCommandDetails();

            commandDetails.FromTransaction = true;

            var commandResult = await commandContext.Receiver.ExecuteCommand(
                commandContext.Socket, commandDetails, []);
            
            commandResults.Add(commandResult);
        }

        var sb = new StringBuilder($"*{commandResults.Count}\r\n");
        foreach (var commandResult in commandResults)
        {
            sb.Append($"{commandResult}\r\n");
        }
        
        commandContext.Socket.Send(Encoding.UTF8.GetBytes(sb.ToString()));
        commandContext.CommandQueue.Clear();
        
        return string.Empty;
    }
}