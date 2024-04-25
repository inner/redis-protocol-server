using System.Net.Sockets;
using System.Text;

namespace codecrafters_redis.Commands;

public class Psync : Base
{
    public override bool IsPropagated => false;

    public override void Execute(Socket socket, int commandCount, string[] commandParts)
    {
        var response = $"+FULLRESYNC {ServerInfo.MasterReplId} 0\r\n";
        socket.Send(Encoding.UTF8.GetBytes(response));
        
        var emptyRdbFileBase64 = "UkVESVMwMDEx+glyZWRpcy12ZXIFNy4yLjD6CnJlZGlzLWJpdHPAQPoFY3RpbWXCbQi8ZfoIdXNlZC1tZW3CsMQQAPoIYW9mLWJhc2XAAP/wbjv+wP9aog==";
        var rdbFile = Convert.FromBase64String(emptyRdbFileBase64);
        var rdbResynchronizationFileMsg = Encoding.ASCII.GetBytes($"${rdbFile.Length}\r\n")
            .Concat(rdbFile)
            .ToArray();
        
        socket.Send(rdbResynchronizationFileMsg);
    }
}