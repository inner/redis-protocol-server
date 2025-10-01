using System.IO.Pipelines;
using System.Net.Sockets;

namespace Redis.Sockets;

public class Connection : IAsyncDisposable
{
    private const int MinBuffSize = 1024;
    private readonly Socket socket;
    private readonly Receiver receiver;
    private Sender? sender;
    private readonly SenderPool senderPool;
    private Task? receiveTask;
    private Task? sendTask;
    private readonly Pipe transportPipe;
    private readonly Pipe applicationPipe;
    private readonly object shutdownLock = new();
    private volatile bool socketDisposed;
    public PipeWriter Output { get; }
    public PipeReader Input { get; }

    public Connection(Socket socket, SenderPool senderPool)
    {
        this.socket = socket;
        receiver = new Receiver();
        this.senderPool = senderPool;
        transportPipe = new Pipe();
        Output = transportPipe.Writer;
        applicationPipe = new Pipe();
        Input = applicationPipe.Reader;
    }

    public void Start()
    {
        try
        {
            sendTask = SendLoop();
            receiveTask = ReceiveLoop();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private async Task SendLoop()
    {
        try
        {
            while (true)
            {
                var result = await transportPipe.Reader.ReadAsync();
                if (result.IsCanceled)
                {
                    break;
                }

                var buff = result.Buffer;
                if (!buff.IsEmpty)
                {
                    sender = senderPool.Rent();
                    await sender.SendAsync(socket, result.Buffer);
                    senderPool.Return(sender);
                    sender = null;
                }

                transportPipe.Reader.AdvanceTo(buff.End);
                if (result.IsCompleted)
                {
                    break;
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        finally
        {
            await applicationPipe.Writer.CompleteAsync();
            Shutdown();
        }
    }

    private async Task ReceiveLoop()
    {
        try
        {
            while (true)
            {
                var buff = applicationPipe.Writer.GetMemory(MinBuffSize);
                var bytes = await receiver.ReceiveAsync(socket, buff);
                if (bytes == 0)
                {
                    break;
                }

                applicationPipe.Writer.Advance(bytes);
                var result = await applicationPipe.Writer.FlushAsync();
                if (result.IsCanceled || result.IsCompleted)
                {
                    break;
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        finally
        {
            await applicationPipe.Writer.CompleteAsync();
            Shutdown();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await transportPipe.Reader.CompleteAsync();
        await applicationPipe.Writer.CompleteAsync();
        try
        {
            if (receiveTask != null)
            {
                await receiveTask;
            }

            if (sendTask != null)
            {
                await sendTask;
            }
        }
        finally
        {
            receiver.Dispose();
            sender?.Dispose();
        }
    }

    public void Shutdown()
    {
        lock (shutdownLock)
        {
            if (socketDisposed)
            {
                return;
            }

            socketDisposed = true;
            try
            {
                socket.Shutdown(SocketShutdown.Both);
            }
            finally
            {
                socket.Dispose();
            }
        }
    }
}