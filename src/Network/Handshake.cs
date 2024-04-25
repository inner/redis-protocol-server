using System.Net.Sockets;
using System.Text;

namespace codecrafters_redis.Network;

public class Handshake
{
    private readonly TcpClient tcpClient;
    
    public Handshake(string host, int port)
    {
        tcpClient = new TcpClient(host, port);
    }
    
    public void Ping()
    {
        var stream = tcpClient.GetStream();

        var pingCommand = "*1\r\n$4\r\nPING\r\n";
        var pingBytes = Encoding.UTF8.GetBytes(pingCommand);

        stream.Write(pingBytes, 0, pingBytes.Length);
    }
}