using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Info : Base
{
    public override bool CanBePropagated => false;

    protected override Task<string> OnMasterNodeExecute(CommandContext commandContext)
    {
        var infoValues = new Dictionary<string, string>
        {
            { "role", "master" },
            { "master_replid", ServerInfo.ServerRuntimeContext.MasterReplId },
            { "master_repl_offset", "0" },
            { "connected_slaves", "0" },
            { "second_repl_offset", "-1" },
            { "repl_backlog_active", "0" },
            { "repl_backlog_size", "1048576" },
            { "repl_backlog_first_byte_offset", "0" },
            { "repl_backlog_histlen", string.Empty }
        };
        
        var infoValue = string.Join('\n', infoValues.Select(x => $"{x.Key}:{x.Value}"));
        var response = RespBuilder.BulkString(infoValue);

        if (!commandContext.ReplicaConnection)
        {
            commandContext.Socket.SendCommand(response);
        }
        
        return Task.FromResult(response);
    }

    protected override Task<string> OnReplicaNodeExecute(CommandContext commandContext)
    {
        var infoValues = new Dictionary<string, string>
        {
            { "role", "slave" },
            { "master_replid", string.Empty },
            { "master_repl_offset", "0" },
            { "connected_slaves", "0" },
            { "second_repl_offset", "-1" },
            { "repl_backlog_active", "0" },
            { "repl_backlog_size", "1048576" },
            { "repl_backlog_first_byte_offset", "0" },
            { "repl_backlog_histlen", string.Empty }
        };
        
        var infoValue = string.Join('\n', infoValues.Select(x => $"{x.Key}:{x.Value}"));
        var response = RespBuilder.BulkString(infoValue);

        if (!commandContext.ReplicaConnection)
        {
            commandContext.Socket.SendCommand(response);
        }
        
        return Task.FromResult(response);
    }
}