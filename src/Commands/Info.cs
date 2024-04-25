using System.Net.Sockets;
using System.Text;

namespace codecrafters_redis.Commands;

public class Info : Base
{
    public override bool IsPropagated => false;
    
    public override void Execute(Socket socket, int commandCount, string[] commandParts)
    {
        var infoValues = new Dictionary<string, string?>
        {
            { "role", ServerInfo.IsMaster ? "master" : "slave" },
            { "master_replid", ServerInfo.IsMaster ? ServerInfo.MasterReplId : string.Empty },
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
        
        socket.Send(Encoding.UTF8.GetBytes(response));
    }
}