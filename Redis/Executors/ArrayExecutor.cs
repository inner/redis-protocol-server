using System.Net.Sockets;
using System.Text.RegularExpressions;
using Redis.Commands.Common;
using Redis.Common;
using Redis.Receivers;

namespace Redis.Executors;

public class ArrayExecutor : IRespDataTypeExecutor
{
    public async Task Execute(Socket socket, string resp, List<CommandQueueItem> commandQueue, ReceiverBase receiver)
    {
        var multiCommandSplit = Regex.Split(resp, @"(\*\d+\\r\\n)")
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.TrimEnd())
            .ToList();

        List<string> commandsToExecute = [];

        var skip = 0;
        for (var i = 0; i < multiCommandSplit.Count / 2; i++)
        {
            var commandToExecute = string.Join(
                string.Empty,
                multiCommandSplit
                    .Skip(skip)
                    .Take(2));

            commandsToExecute.Add(commandToExecute);
            skip += 2;
        }

        foreach (var commandToExecute in commandsToExecute)
        {
            var commandDetails = commandToExecute.BuildCommandDetails();
            await receiver.ExecuteCommand(socket, commandDetails, commandQueue);
        }
    }
}