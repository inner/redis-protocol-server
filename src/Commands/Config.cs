using System.Text;

namespace codecrafters_redis.Commands;

public class Config : Base
{
    public override bool CanBePropagated => false;

    protected override async Task<string> OnMasterNodeExecute(CommandContext commandContext)
    {
        return await GenerateCommonResponse(commandContext);
    }

    protected override async Task<string> OnReplicaNodeExecute(CommandContext commandContext)
    {
        return await GenerateCommonResponse(commandContext);
    }

    private static Task<string> GenerateCommonResponse(CommandContext commandContext)
    {
        var result =
            $"*2\r\n$3\r\ndir\r\n${ServerInfo.ServerRuntimeContext.DataDir.Length}\r\n{ServerInfo.ServerRuntimeContext.DataDir}\r\n";

        if (Array.IndexOf(commandContext.CommandDetails.CommandParts, "GET") != -1 &&
            Array.IndexOf(commandContext.CommandDetails.CommandParts, "dir") != -1)
        {
            if (!commandContext.ReplicaConnection)
            {
                commandContext.Socket.Send(Encoding.UTF8.GetBytes(result));
            }

            return Task.FromResult(result);
        }

        if (Array.IndexOf(commandContext.CommandDetails.CommandParts, "GET") != -1 &&
            Array.IndexOf(commandContext.CommandDetails.CommandParts, "dbfilename") != -1)
        {
            result =
                $"*2\r\n$10\r\ndbfilename\r\n${ServerInfo.ServerRuntimeContext.DbFilename.Length}\r\n{ServerInfo.ServerRuntimeContext.DbFilename}\r\n";

            if (!commandContext.ReplicaConnection)
            {
                commandContext.Socket.Send(
                    Encoding.UTF8.GetBytes(result));
            }

            return Task.FromResult(result);
        }

        result = "-ERR Unsupported CONFIG parameter\r\n";

        if (!commandContext.ReplicaConnection)
        {
            commandContext.Socket.Send(Encoding.UTF8.GetBytes(result));
        }

        return Task.FromResult(result);
    }
}