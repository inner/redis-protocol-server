using codecrafters_redis.Common;

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
        var resp = RespBuilder.BuildRespArray("dir", ServerInfo.ServerRuntimeContext.DataDir);
        
        if (Array.IndexOf(commandContext.CommandDetails.CommandParts, "GET") != -1 &&
            Array.IndexOf(commandContext.CommandDetails.CommandParts, "dir") != -1)
        {
            if (!commandContext.ReplicaConnection)
            {
                commandContext.Socket.Send(resp.AsBytes());
            }

            return Task.FromResult(resp);
        }

        if (Array.IndexOf(commandContext.CommandDetails.CommandParts, "GET") != -1 &&
            Array.IndexOf(commandContext.CommandDetails.CommandParts, "dbfilename") != -1)
        {
            resp = RespBuilder.BuildRespArray("dbfilename", ServerInfo.ServerRuntimeContext.DbFilename);
            
            if (!commandContext.ReplicaConnection)
            {
                commandContext.Socket.Send(resp.AsBytes());
            }

            return Task.FromResult(resp);
        }
        
        resp = RespBuilder.BuildRespError("Unsupported CONFIG parameter");
        
        if (!commandContext.ReplicaConnection)
        {
            commandContext.Socket.Send(resp.AsBytes());
        }

        return Task.FromResult(resp);
    }
}