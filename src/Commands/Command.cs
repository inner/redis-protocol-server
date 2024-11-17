using System.Text;
using codecrafters_redis.Commands.Common;
using codecrafters_redis.Common;

namespace codecrafters_redis.Commands;

public class Command : Base
{
    public override bool CanBePropagated => false;

    protected override async Task<string> OnMasterNodeExecute(CommandContext commandContext)
    {
        string resp;

        var commandPartsLength = commandContext.CommandDetails.CommandParts.Length;

        if (commandPartsLength < 5 || !commandContext.CommandDetails.CommandParts[4]
                .Equals("DOCS", StringComparison.CurrentCultureIgnoreCase))
        {
            resp = RespBuilder.Error("Invalid command");
            commandContext.Socket.Send(resp.AsBytes());
            return resp;
        }

        resp = await GetDocsResp(commandPartsLength == 7
            ? commandContext.CommandDetails.CommandParts[6]
            : null);

        commandContext.Socket.Send(resp.AsBytes());
        return resp;
    }

    private static Task<string> GetDocsResp(string? commandKey = null)
    {
        var commands = new Dictionary<string, Dictionary<string, string>>
        {
            {
                "GET",
                new Dictionary<string, string>
                {
                    { "summary", "Returns the string value of a key." },
                    { "since", "1.0.0" },
                    { "group", "string" }
                }
            },
            {
                "SET",
                new Dictionary<string, string>
                {
                    {
                        "summary",
                        "Sets the string value of a key, ignoring its type. The key is created if it doesn't exist."
                    },
                    { "since", "1.0.0" },
                    { "group", "string" }
                }
            }
        };

        var filteredCommands = commandKey == null
            ? commands
            : commands.Where(c => c.Key.Equals(commandKey, StringComparison.CurrentCultureIgnoreCase))
                .ToDictionary(c => c.Key, c => c.Value);

        if (filteredCommands.Count == 0)
        {
            return Task.FromResult(RespBuilder.EmptyArray());
        }

        var sb = new StringBuilder();
        sb.Append($"*{filteredCommands.Count * 2}\r\n");
        foreach (var command in filteredCommands)
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