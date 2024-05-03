using System.Net;
using System.Net.Sockets;
using System.Text;
using codecrafters_redis.Receivers;

namespace codecrafters_redis.Servers;

public abstract class NodeBase
{
    private readonly IPAddress localAddress;
    private readonly int port;

    private readonly ReceiverBase receiver;

    protected NodeBase(IPAddress localAddress, int port, ReceiverBase receiver)
    {
        this.localAddress = localAddress;
        this.port = port;
        this.receiver = receiver;
    }

    protected abstract void LogOnStart();
    protected abstract string NodeName { get; }

    public void Start()
    {
        LogOnStart();

        try
        {
            var server = new TcpListener(localAddress, port);
            server.Start();

            while (true)
            {
                var client = server.AcceptTcpClient();
                _ = Task.Run(() => HandleConnection(client));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}, stack: {ex.StackTrace}");
            throw;
        }
    }

    private void LogReceivedCommand(string clientCommand)
    {
        var logMessage = clientCommand.Replace("\r\n", "\\r\\n");

        if (!logMessage.EndsWith('\n'))
        {
            logMessage += '\n';
        }

        Console.WriteLine($"[{NodeName}] Received command: {logMessage[..^1]}.");
    }

    protected void HandleConnection(TcpClient client)
    {
        var connectionId = $"{client.Client.LocalEndPoint}->{client.Client.RemoteEndPoint}";

        while (client.Connected)
        {
            try
            {
                Console.WriteLine($"TCP Connection [{connectionId}] established");

                while (true)
                {
                    var buffer = new byte[1024];
                    var bytesRead = client.GetStream().Read(buffer, 0, buffer.Length);
                    var clientCommand = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    
                    //var clientCommand = Encoding.UTF8.GetString(buffer, 0, bytesReceived);

                    LogReceivedCommand(clientCommand);

                    if (string.IsNullOrWhiteSpace(clientCommand))
                    {
                        client.Client.Send(Encoding.UTF8.GetBytes(Constants.NullResponse));
                        continue;
                    }
                    
                    receiver.Receive(client.Client, clientCommand);
                }
            }
            catch (SocketException)
            {
                Console.WriteLine($"Closing TCP connection: [{connectionId}]");
            }
            finally
            {
                CloseSocket(connectionId, client.Client);
            }
        }
    }

    private void CloseSocket(string connectionId, Socket? socket)
    {
        if (socket == null)
        {
            return;
        }

        Console.WriteLine($"TCP Connection [{connectionId}] closed");
        socket.Close();
    }
}