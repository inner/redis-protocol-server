using System.Net;
using System.Net.Sockets;
using System.Text;

namespace codecrafters_redis.Servers;

public abstract class NodeBase
{
    private readonly IPAddress localAddress;
    private readonly int port;

    private readonly Receiver receiver;

    protected NodeBase(IPAddress localAddress, int port, Receiver receiver)
    {
        this.localAddress = localAddress;
        this.port = port;
        this.receiver = receiver;
    }

    protected abstract void LogOnStart();

    public void Start()
    {
        LogOnStart();

        try
        {
            var server = new TcpListener(localAddress, port);
            server.Start();

            while (true)
            {
                var socket = server.AcceptSocket();
                _ = Task.Run(() => HandleConnection(socket));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}, stack: {ex.StackTrace}");
            throw;
        }
    }

    private void LogReceivedCommand(string s)
    {
        var logMessage = s.Replace("\r\n", "\\r\\n");

        if (!logMessage.EndsWith('\n'))
        {
            logMessage += '\n';
        }

        Console.Write($"Received command: {logMessage}");
    }

    protected void HandleConnection(Socket socket)
    {
        var connectionId = $"{socket.LocalEndPoint}->{socket.RemoteEndPoint}";

        while (socket.Connected)
        {
            try
            {
                // Console.WriteLine($"TCP Connection [{connectionId}] established");

                while (true)
                {
                    var buffer = new byte[1024];
                    var bytesReceived = socket.Receive(buffer);
                    var clientCommand = Encoding.UTF8.GetString(buffer, 0, bytesReceived);

                    //LogReceivedCommand(clientCommand);

                    if (string.IsNullOrWhiteSpace(clientCommand))
                    {
                        //socket.Send(Encoding.UTF8.GetBytes(Environment.NewLine));
                        socket.Send(Encoding.UTF8.GetBytes(Constants.NullResponse));
                        continue;
                    }

                    receiver.Receive(socket, clientCommand);
                }
            }
            catch (SocketException)
            {
                Console.WriteLine($"Closing TCP connection: [{connectionId}]");
            }
            finally
            {
                CloseSocket(connectionId, socket);
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