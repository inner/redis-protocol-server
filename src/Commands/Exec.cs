using System.Net.Sockets;
using System.Text;
using codecrafters_redis.Common;
using codecrafters_redis.Receivers;

namespace codecrafters_redis.Commands;

public class Exec : Base
{
    public override bool CanBePropagated => true;

    protected override async Task<string> OnMasterNodeExecute(Socket socket, CommandDetails commandDetails,
        List<CommandQueueItem> commandQueue, ReceiverBase receiver, bool replicaConnection = false)
    {
        return await GenerateCommonResponse(socket, commandQueue, receiver);
    }

    protected override async Task<string> OnReplicaNodeExecute(Socket socket, CommandDetails commandDetails,
        List<CommandQueueItem> commandQueue, ReceiverBase receiver, bool replicaConnection = false)
    {
        return await GenerateCommonResponse(socket, commandQueue, receiver);
    }

    private static async Task<string> GenerateCommonResponse(Socket socket, List<CommandQueueItem> commandQueue,
        ReceiverBase receiver)
    {
        string result;

        if (commandQueue.All(x => x.CommandType != CommandType.Multi))
        {
            result = "-ERR EXEC without MULTI\r\n";
            socket.Send(Encoding.UTF8.GetBytes(result));
            return result;
        }

        if (commandQueue.Count == 1 && commandQueue.Single().CommandType == CommandType.Multi)
        {
            result = "*0\r\n";
            socket.Send(Encoding.UTF8.GetBytes(result));
            commandQueue.Clear();
            return result;
        }

        List<string> commandResults = [];

        foreach (var commandInQueue in commandQueue)
        {
            if (commandInQueue.CommandType == CommandType.Multi)
            {
                continue;
            }
            
            var commandDetails = commandInQueue.CommandString
                .Replace("\r\n", @"\r\n")
                .BuildCommandDetails();

            commandDetails.FromTransaction = true;

            var commandResult = await receiver.ExecuteCommand(socket, commandDetails, []);
            commandResults.Add(commandResult);
        }
        
        var sb = new StringBuilder();
        sb.Append($"*{commandResults.Count}\r\n");
        foreach (var commandResult in commandResults)
        {
            sb.Append($"{commandResult}\r\n");
        }
        
        socket.Send(Encoding.UTF8.GetBytes(sb.ToString()));
        commandQueue.Clear();
        return string.Empty;
    }
}