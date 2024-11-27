using System.Text;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

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
        var commands = Docs();

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

    public override Dictionary<string, Dictionary<string, string>> Docs()
    {
        var commands = typeof(Base).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && t.IsSubclassOf(typeof(Base)) &&
                        t.Name != nameof(Command))
            .Select(t => (Base)Activator.CreateInstance(t)!);

        var commandDocs = commands.SelectMany(c => c.Docs()).ToList();
        
        var docs = commandDocs
            .GroupBy(d => d.Key)
            .ToDictionary(g => g.Key, g => g.SelectMany(d => d.Value)
                .ToDictionary(d => d.Key, d => d.Value));

        return docs;
    }
}