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

        if (response != "+PONG\r\n")
        {
            throw new Exception("Handshake failed");
        }

        // send REPLCONF listening-port command
        var replconfListeningPort = "*3\r\n$8\r\nREPLCONF\r\n$14\r\nlistening-port\r\n$4\r\n6380\r\n";
        var replconfBytes = Encoding.UTF8.GetBytes(replconfListeningPort);
        stream.Write(replconfBytes, 0, replconfBytes.Length);

        bytesRead = stream.Read(buffer, 0, buffer.Length);
        response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

        if (response != "+OK\r\n")
        {
            throw new Exception("Handshake failed");
        }

        // send REPLCONF capa command
        var replconfCapa = "*3\r\n$8\r\nREPLCONF\r\n$4\r\ncapa\r\n$6\r\npsync2\r\n";
        var replconfCapaBytes = Encoding.UTF8.GetBytes(replconfCapa);
        stream.Write(replconfCapaBytes, 0, replconfCapaBytes.Length);

        bytesRead = stream.Read(buffer, 0, buffer.Length);
        response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

        if (response != "+OK\r\n")
        {
            throw new Exception("Handshake failed");
        }

        // send REPLCONF psync command
        var replconfPsync = "*3\r\n$5\r\nPSYNC\r\n$1\r\n?\r\n$2\r\n-1\r\n";
        var replconfPsyncBytes = Encoding.UTF8.GetBytes(replconfPsync);
        stream.Write(replconfPsyncBytes, 0, replconfPsyncBytes.Length);

        bytesRead = stream.Read(buffer, 0, buffer.Length);
        response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

        if (!response.Contains("FULLRESYNC"))
        {
            throw new Exception("Handshake failed");
        }
        
        Task.Run(() => NewMethod(tcpClient.Client));
    }

    private Task NewMethod(Socket socket)
    {
        var receiver = new Receiver();
        
        while (socket.Connected)
        {
            var buffer = new byte[1024];
            var bytesReceived = socket.Receive(buffer);
            var clientCommand = Encoding.UTF8.GetString(buffer, 0, bytesReceived);

            LogReceivedCommand(clientCommand);

            if (string.IsNullOrWhiteSpace(clientCommand))
            {
                socket.Send(Encoding.UTF8.GetBytes(Environment.NewLine));
                continue;
            }

            receiver.Receive(socket, clientCommand);
        }
        
        return Task.CompletedTask;
    }

    void LogReceivedCommand(string s)
    {
        var logMessage = s.Replace("\r\n", "\\r\\n");

        if (!logMessage.EndsWith('\n'))
        {
            logMessage += '\n';
        }
    
        Console.Write($"Received command2: {logMessage}");
    }
}