using System.Net;
using System.Net.Sockets;
using codecrafters_redis.Commands.Common;
using codecrafters_redis.Common;
using codecrafters_redis.Rdb;
using codecrafters_redis.Receivers;

namespace codecrafters_redis.Nodes;

public abstract class NodeBase(IPAddress localAddress, int port, ReceiverBase receiver)
{
    protected abstract void LogOnStart();
    protected abstract string NodeName { get; }

    public void Start()
    {
        LogOnStart();

        try
        {
            var server = new TcpListener(localAddress, port);
            server.Start();

            if (ServerInfo.ServerRuntimeContext.DbFileExists)
            {
                var path = Path.Combine(
                    ServerInfo.ServerRuntimeContext.DataDir,
                    ServerInfo.ServerRuntimeContext.DbFilename);

                RdbReader.ReadRdb(path);
            }

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
        var logMessage = clientCommand.Replace("\r\n", @"\r\n");
        Console.WriteLine($"[{NodeName}] Received command: {logMessage}.");
    }

    protected async Task HandleConnection(TcpClient client)
    {
        List<CommandQueueItem> commandQueue = [];
        var connectionId = $"{client.Client.LocalEndPoint}->{client.Client.RemoteEndPoint}";

        while (client.Connected)
        {
            try
            {
                Console.WriteLine($"[{NodeName}] TCP Connection [{connectionId}] established");

                while (true)
                {
                    var clientCommand = client
                        .GetStream()
                        .ReadResponse();

                    if (string.IsNullOrEmpty(clientCommand))
                    {
                        client.Client.Send(RespBuilder.Null().AsBytes());
                        continue;
                    }

                    await receiver.Receive(client.Client, clientCommand, commandQueue);
                    LogReceivedCommand(clientCommand);
                }
            }
            catch (SocketException)
            {
                Console.WriteLine($"Closing TCP connection: [{connectionId}]");
            }
            catch (IOException)
            {
                Console.WriteLine($"Client closed connection (I/O): [{connectionId}]");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}, stack: {ex.StackTrace}");
            }
            finally
            {
                CloseTcpClient(connectionId, client);
            }
        }
    }

    private static void CloseTcpClient(string connectionId, TcpClient? tcpClient)
    {
        if (tcpClient is not { Connected: true }) return;
        
        tcpClient.Client.Close();
        
        Console.WriteLine($"TCP Connection [{connectionId}] closed");
    }
}