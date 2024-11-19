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
        var multiRespSplit = Regex.Split(resp, @"(\*\d+\\r\\n)")
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.TrimEnd())
            .ToList();

        List<string> respCommands = [];

        var skip = 0;
        for (var i = 0; i < multiRespSplit.Count / 2; i++)
        {
            var commandToExecute = string.Join(
                string.Empty,
                multiRespSplit
                    .Skip(skip)
                    .Take(2));

            respCommands.Add(commandToExecute);
            skip += 2;
        }

        foreach (var respCommand in respCommands)
        {
            var commandDetails = respCommand.BuildCommandDetails();
            await receiver.ExecuteCommand(socket, commandDetails, commandQueue);
        }
    }
}