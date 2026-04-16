using System.Text;
using Redis.CommandDocs;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Command : Base
{
    protected override string Name => nameof(Command);
    public override bool CanBePropagated => false;

    protected override async Task<string> OnMasterNodeExecute(CommandContext commandContext)
    {
        string resp;

        var commandPartsLength = commandContext.CommandDetails.CommandParts.Length;

        if (commandPartsLength < 5 || !commandContext.CommandDetails.CommandParts[4]
                .Equals("DOCS", StringComparison.CurrentCultureIgnoreCase))
        {
            resp = RespBuilder.Error("Invalid command");
            commandContext.Socket.SendCommand(resp);
            return resp;
        }

        resp = await GetDocsResp(commandPartsLength == 7
            ? commandContext.CommandDetails.CommandParts[6]
            : null);

        commandContext.Socket.SendCommand(resp);
        return resp;
    }

    private Task<string> GetDocsResp(string? commandKey = null)
    {
        var commands = CommandDocsRegistry.AllDocs();

        var commandsFiltered = commandKey == null
            ? commands
            : commands.Where(c => c.Key.Equals(commandKey, StringComparison.CurrentCultureIgnoreCase))
                .ToDictionary(c => c.Key, c => c.Value);

        if (commandsFiltered.Count == 0)
        {
            return Task.FromResult(RespBuilder.EmptyArray());
        }

        var sb = new StringBuilder();
        sb.Append($"*{commandsFiltered.Count * 2}\r\n");
        foreach (var command in commandsFiltered)
        {
            sb.Append(RespBuilder.BulkString(command.Key));
            sb.Append($"*{command.Value.Count * 2}\r\n");
            foreach (var subCommand in command.Value)
            {
                sb.Append(RespBuilder.BulkString(subCommand.Key));
                sb.Append(RespBuilder.BulkString(subCommand.Value));
            }
        }

        return Task.FromResult(sb.ToString());
    }
}
