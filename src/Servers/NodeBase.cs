using System.Net;
using System.Net.Sockets;
using System.Text;
using codecrafters_redis.Cache;
using codecrafters_redis.Rdb;
using codecrafters_redis.Receivers;
using RDBParser;

namespace codecrafters_redis.Servers;

public class MyReaderCallBack : IReaderCallback
{
    public void StartRDB(int version)
    {
        Console.WriteLine($"Version: {version}");
    }

    public void FunctionLoad(byte[] engine, byte[] libName, byte[] code)
    {
        throw new NotImplementedException();
    }

    public void AuxField(byte[] key, byte[] value)
    {
        Console.WriteLine($"AuxField: {Encoding.UTF8.GetString(key)} - {Encoding.UTF8.GetString(value)}");
    }

    public void StartDatabase(int database)
    {
        Console.WriteLine($"Database: {database}");
    }

    public bool StartModule(byte[] key, string module_name, long expiry, Info info)
    {
        throw new NotImplementedException();
    }

    public void HandleModuleData(byte[] key, ulong opCode, byte[] data)
    {
        throw new NotImplementedException();
    }

    public void EndModule(byte[] key, long bufferSize, byte[] buffer)
    {
        throw new NotImplementedException();
    }

    public void DbSize(uint dbSize, uint expiresSize)
    {
        Console.WriteLine($"DbSize: {dbSize} - {expiresSize}");
    }

    public void Set(byte[] key, byte[] value, long expiry, Info info)
    {
        Console.WriteLine($"Set: {Encoding.UTF8.GetString(key)} - {Encoding.UTF8.GetString(value)}");
        DataCache.Set(Encoding.UTF8.GetString(key), Encoding.UTF8.GetString(value), (int)expiry);
    }

    public void StartHash(byte[] key, long length, long expiry, Info info)
    {
        throw new NotImplementedException();
    }

    public void HSet(byte[] key, byte[] field, byte[] value)
    {
        throw new NotImplementedException();
    }

    public void EndHash(byte[] key)
    {
        throw new NotImplementedException();
    }

    public void StartSet(byte[] key, long cardinality, long expiry, Info info)
    {
        throw new NotImplementedException();
    }

    public void SAdd(byte[] key, byte[] member)
    {
        throw new NotImplementedException();
    }

    public void EndSet(byte[] key)
    {
        throw new NotImplementedException();
    }

    public void StartList(byte[] key, long expiry, Info info)
    {
        throw new NotImplementedException();
    }

    public void RPush(byte[] key, byte[] value)
    {
        throw new NotImplementedException();
    }

    public void EndList(byte[] key, Info info)
    {
        throw new NotImplementedException();
    }

    public void StartSortedSet(byte[] key, long length, long expiry, Info info)
    {
        throw new NotImplementedException();
    }

    public void ZAdd(byte[] key, double score, byte[] member)
    {
        throw new NotImplementedException();
    }

    public void EndSortedSet(byte[] key)
    {
        throw new NotImplementedException();
    }

    public void StartStream(byte[] key, long listpacksCount, long expiry, Info info)
    {
        throw new NotImplementedException();
    }

    public void StreamListPack(byte[] key, byte[] entryId, byte[] data)
    {
        throw new NotImplementedException();
    }

    public void EndStream(byte[] key, StreamEntity entity)
    {
        throw new NotImplementedException();
    }

    public void EndDatabase(int dbNumber)
    {
        Console.WriteLine($"EndDatabase: {dbNumber}");
    }

    public void EndRDB()
    {
        Console.WriteLine("EndRDB");
    }

    public void SetIdleOrFreq(int val)
    {
        throw new NotImplementedException();
    }
}

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

            var path = Path.Combine(
                ServerInfo.ServerRuntimeContext.DataDir,
                ServerInfo.ServerRuntimeContext.DbFilename);
            
            // var cb = new MyReaderCallBack();
            // var parser = new RDBParser.BinaryReaderRDBParser(cb);
            // parser.Parse(path);
            
            RdbReader.ReadRdb(path);

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

    protected async Task HandleConnection(TcpClient client)
    {
        var connectionId = $"{client.Client.LocalEndPoint}->{client.Client.RemoteEndPoint}";

        while (client.Connected)
        {
            try
            {
                Console.WriteLine($"[{NodeName}] TCP Connection [{connectionId}] established");

                while (true)
                {
                    var buffer = new byte[1024];
                    var bytesRead = client.GetStream().Read(buffer, 0, buffer.Length);
                    var clientCommand = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    if (string.IsNullOrEmpty(clientCommand))
                    {
                        client.Client.Send(Encoding.UTF8.GetBytes(Constants.NullResponse));
                        continue;
                    }

                    LogReceivedCommand(clientCommand);
                    
                    await receiver.Receive(client.Client, clientCommand.Replace("\"", string.Empty));
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