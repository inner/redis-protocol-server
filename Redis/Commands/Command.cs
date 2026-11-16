using System.Text;
using Redis.CommandDocs;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Command : Base
{
    protected override string Name => nameof(Command);
    public override bool CanBePropagated => false;

    protected override async Task<string> ExecuteCore(CommandContext commandContext)
    {
        var parts = commandContext.CommandDetails.CommandParts;
        var subcommand = parts.Length > 4 ? parts[4].ToUpperInvariant() : string.Empty;

        // TODO: bare COMMAND and COMMAND INFO (per-command arity, flags and key
        // specs) are not supported.
        var resp = subcommand switch
        {
            "DOCS" => await GetDocsResp(parts.Length == 7 ? parts[6] : null),
            "COUNT" => RespBuilder.Integer(SupportedCommandNames().Length),
            "LIST" => ListResp(),
            _ => RespBuilder.Error(
                $"unknown subcommand or wrong number of arguments for '{subcommand}'. Try COMMAND HELP.")
        };

        Reply(commandContext, resp);
        return resp;
    }

    // A command is supported when it has an implementation class; some
    // RespType values (e.g. PSUBSCRIBE) are only declared.
    private static string[] SupportedCommandNames()
    {
        return Enum.GetNames<RespType>()
            .Where(name => System.Type.GetType($"Redis.Commands.{name}") != null)
            .Select(name => name.ToLowerInvariant())
            .ToArray();
    }

    private static string ListResp()
    {
        var names = SupportedCommandNames();
        var sb = new StringBuilder(RespBuilder.InitArray(names.Length));
        foreach (var name in names)
        {
            sb.Append(RespBuilder.BulkString(name));
        }

        return sb.ToString();
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
