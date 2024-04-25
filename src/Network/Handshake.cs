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
    
    public void Start()
    {
        // send ping command
        var stream = tcpClient.GetStream();

        var pingCommand = "*1\r\n$4\r\nPING\r\n";
        var pingBytes = Encoding.UTF8.GetBytes(pingCommand);
        stream.Write(pingBytes, 0, pingBytes.Length);
        
        // get response
        var buffer = new byte[1024];
        var bytesRead = stream.Read(buffer, 0, buffer.Length);
        var response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        
        if (response != Constants.PongResponse)
        {
            throw new Exception("Handshake failed");
        }
        
        // send REPLCONF listening-port command
        var replconfListeningPort = $"*3\r\n$7\r\nREPLCONF\r\n$9\r\nlistening\r\n$4\r\n{6380}\r\n";
        var replconfBytes = Encoding.UTF8.GetBytes(replconfListeningPort);
        stream.Write(replconfBytes, 0, replconfBytes.Length);
            
        bytesRead = stream.Read(buffer, 0, buffer.Length);
        response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            
        if (response != Constants.OkResponse)
        {
            throw new Exception("Handshake failed");
        }
        
        // send REPLCONF capa command
        var replconfCapa = "*3\r\n$8\r\nREPLCONF\r\n$14\r\nlistening-port\r\n$4\r\n6380\r\n";
        var replconfCapaBytes = Encoding.UTF8.GetBytes(replconfCapa);
        stream.Write(replconfCapaBytes, 0, replconfCapaBytes.Length);
        
        bytesRead = stream.Read(buffer, 0, buffer.Length);
        response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        
        if (response != Constants.OkResponse)
        {
            throw new Exception("Handshake failed");
        }
    }
}