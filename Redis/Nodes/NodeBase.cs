using System.Net;
using System.Net.Sockets;
using Redis.Cache;
using Redis.Commands.Common;
using Redis.Common;
using Redis.Rdb;
using Redis.Receivers;

namespace Redis.Nodes;

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

    private void LogReceivedCommand(string resp)
    {
        var logMessage = resp.Replace(Constants.NewLine, Constants.VerbatimNewLine);
        Console.WriteLine($"[{NodeName}] Received command: [{logMessage}].");
    }

    protected async Task HandleConnection(TcpClient client)
    {
        List<CommandQueueItem> commandQueue = [];
        List<string> subscriptions = [];

        var socket = client.Client;
        var connectionId = $"{socket.LocalEndPoint}->{socket.RemoteEndPoint}";
        var commandSource = ResolveCommandSource(socket);
        var reader = new RespReader(client.GetStream());

        // As in Redis, a connection opened while the default user has no
        // password is logged in as `default`, and stays logged in if a
        // password is set later. Connections opened after that must AUTH.
        if (!DataCache.DefaultUserExists())
        {
            DataCache.AddAuthenticatedConnection("default", socket);
        }

        Console.WriteLine($"[{NodeName}] TCP Connection [{connectionId}] established");

        try
        {
            while (true)
            {
                string? resp;
                try
                {
                    resp = reader.ReadFrame();
                }
                catch (RespParser.ProtocolException ex)
                {
                    // Like Redis: report and drop the connection, since the
                    // stream can no longer be framed reliably.
                    socket.SendCommand(RespBuilder.Error($"Protocol error: {ex.Message}"));
                    break;
                }

                // The peer closed its side of the connection.
                if (resp == null)
                {
                    break;
                }

                LogReceivedCommand(resp);
                await receiver.Receive(socket, resp, commandQueue, subscriptions, commandSource);
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
            // Per-connection state lives in global registries keyed by
            // socket; drop it so closed connections do not accumulate.
            DataCache.RemoveConnection(socket);
            WatchRegistry.Unwatch(socket);

            foreach (var replica in ServerInfo.ServerRuntimeContext.Replicas.Where(x => x.Value == socket))
            {
                ServerInfo.ServerRuntimeContext.Replicas.TryRemove(replica.Key, out _);
            }

            CloseTcpClient(connectionId, client);
        }
    }

    private static CommandSource ResolveCommandSource(Socket socket)
    {
        return !ServerInfo.ServerRuntimeContext.IsMaster && ServerInfo.ServerRuntimeContext.MasterSocket != null &&
               ReferenceEquals(socket, ServerInfo.ServerRuntimeContext.MasterSocket)
            ? CommandSource.ReplicationMaster
            : CommandSource.Client;
    }

    private static void CloseTcpClient(string connectionId, TcpClient tcpClient)
    {
        // Always close, even when the peer already disconnected, so the
        // socket handle is released.
        tcpClient.Close();
        Console.WriteLine($"TCP Connection [{connectionId}] closed");
    }
}
