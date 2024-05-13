using System.Net.Sockets;
using System.Text;

namespace codecrafters_redis.Commands;

public class Info : Base
{
    public override bool CanBePropagated => false;

    protected override Task OnMasterNodeExecute(Socket socket, int commandCount, string[] commandParts,
        bool replicaConnection = false)
    {
        var infoValues = new Dictionary<string, string?>
        {
            { "role", "master" },
            { "master_replid", ServerInfo.MasterReplId },
            { "master_repl_offset", "0" },
            { "connected_slaves", "0" },
            { "second_repl_offset", "-1" },
            { "repl_backlog_active", "0" },
            { "repl_backlog_size", "1048576" },
            { "repl_backlog_first_byte_offset", "0" },
            { "repl_backlog_histlen", string.Empty }
        };
        
        var value = string.Join('\n', infoValues.Select(x => $"{x.Key}:{x.Value}"));
        var response = $"${value.Length}\r\n{value}\r\n";

        if (!replicaConnection)
        {
            socket.Send(Encoding.UTF8.GetBytes(response));
        }
        
        return Task.CompletedTask;
    }

    protected override Task OnReplicaNodeExecute(Socket socket, int commandCount, string[] commandParts,
        bool replicaConnection = false)
    {
        var infoValues = new Dictionary<string, string?>
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
        
        var value = string.Join('\n', infoValues.Select(x => $"{x.Key}:{x.Value}"));
        var response = $"${value.Length}\r\n{value}\r\n";

        if (!replicaConnection)
        {
            socket.Send(Encoding.UTF8.GetBytes(response));
        }
        
        return Task.CompletedTask;
    }
}