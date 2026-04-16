using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Config : Base
{
    protected override string Name => nameof(Config);
    public override bool CanBePropagated => false;

    protected override Task<string> ExecuteCore(CommandContext commandContext)
    {
        var resp = RespBuilder.ArrayFromCommands("dir", ServerInfo.ServerRuntimeContext.DataDir);
        
        if (Array.IndexOf(commandContext.CommandDetails.CommandParts, "GET") != -1 &&
            Array.IndexOf(commandContext.CommandDetails.CommandParts, "dir") != -1)
        {
            commandContext.Socket.SendCommand(resp);
            return Task.FromResult(resp);
        }

        if (Array.IndexOf(commandContext.CommandDetails.CommandParts, "GET") != -1 &&
            Array.IndexOf(commandContext.CommandDetails.CommandParts, "dbfilename") != -1)
        {
            resp = RespBuilder.ArrayFromCommands("dbfilename", ServerInfo.ServerRuntimeContext.DbFilename);
            commandContext.Socket.SendCommand(resp);

            return Task.FromResult(resp);
        }
        
        resp = RespBuilder.Error("Unsupported CONFIG parameter");
        commandContext.Socket.SendCommand(resp);

        return Task.FromResult(resp);
    }
}
