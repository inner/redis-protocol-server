using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Config : Base
{
    protected override string Name => nameof(Config);
    public override bool CanBePropagated => false;

    // Readable parameters, resolved lazily so CONFIG GET always reflects the
    // current runtime values.
    // TODO: glob patterns and multiple parameters per CONFIG GET are not supported.
    private static readonly Dictionary<string, Func<string>> Parameters =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["dir"] = () => ServerInfo.ServerRuntimeContext.DataDir,
            ["dbfilename"] = () => ServerInfo.ServerRuntimeContext.DbFilename,
            ["appendonly"] = () => ServerInfo.ServerRuntimeContext.Aof.AppendOnly,
            ["appenddirname"] = () => ServerInfo.ServerRuntimeContext.Aof.AppendDirName,
            ["appendfilename"] = () => ServerInfo.ServerRuntimeContext.Aof.AppendFilename,
            ["appendfsync"] = () => ServerInfo.ServerRuntimeContext.Aof.AppendFsync,
        };

    protected override Task<string> ExecuteCore(CommandContext commandContext)
    {
        var parts = commandContext.CommandDetails.CommandParts;
        string resp;

        // CONFIG GET parameter -> parts[4] is the subcommand, parts[6] the name.
        if (parts.Length >= 7 &&
            string.Equals(parts[4], "GET", StringComparison.OrdinalIgnoreCase) &&
            Parameters.TryGetValue(parts[6], out var getValue))
        {
            resp = RespBuilder.ArrayFromCommands(parts[6].ToLowerInvariant(), getValue());
        }
        else
        {
            resp = RespBuilder.Error("Unsupported CONFIG parameter");
        }

        SendIfNotFromTransaction(commandContext, resp);
        return Task.FromResult(resp);
    }
}
