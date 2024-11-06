using System.Text;

namespace codecrafters_redis.Commands;

public class Command : Base
{
    public override bool CanBePropagated => false;

    protected override async Task<string> OnMasterNodeExecute(CommandContext commandContext)
    {
        if (commandContext.CommandDetails.CommandParts.Length != 5 ||
            commandContext.CommandDetails.CommandParts[4].ToUpper() != "DOCS")
        {
            throw new ArgumentException("Invalid subcommand for COMMAND.");
        }

        var docs = await GetCommandDocs();
        var result = await FormatAsResp(docs);
        commandContext.Socket.Send(Encoding.UTF8.GetBytes(result));
        return result;
    }

    protected override Task<string> OnReplicaNodeExecute(CommandContext commandContext)
    {
        return Task.FromResult(string.Empty);
    }

    private Task<string> GetCommandDocs()
    {
        var commandDocs = new Dictionary<string, string>
        {
            { "GET", "GET <key>: Get the value of the specified key." },
            { "SET", "SET <key> <value>: Set the value of the specified key." },
            { "DEL", "DEL <key>: Delete the specified key." },
            { "EXISTS", "EXISTS <key>: Determine if a key exists." },
            { "COMMAND DOCS", "COMMAND DOCS: Returns documentation about commands." }
        };

        var docs = new StringBuilder();
        foreach (var entry in commandDocs)
        {
            docs.AppendLine($"{entry.Key}: {entry.Value}");
        }

        return Task.FromResult(docs.ToString());
    }

    private Task<string> FormatAsResp(string response)
    {
        return Task.FromResult($"${response.Length}\r\n{response}\r\n");
    }
}