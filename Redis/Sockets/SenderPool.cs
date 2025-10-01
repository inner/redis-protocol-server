using System.Collections.Concurrent;

namespace Redis.Sockets;

public class SenderPool(int maxNumberOfSenders = 128) : IDisposable
{
    private int count;
    private readonly ConcurrentQueue<Sender> senders = new();
    private bool disposed;

    public Sender Rent()
    {
        if (senders.TryDequeue(out var sender))
        {
            Interlocked.Decrement(ref count);
            sender.Reset();
            return sender;
        }

        return new Sender();
    }

    public void Return(Sender sender)
    {
        if (disposed || count >= maxNumberOfSenders)
        {
            sender.Dispose();
        }
        else
        {
            Interlocked.Increment(ref count);
            senders.Enqueue(sender);
        }
    }

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        while (senders.TryDequeue(out var sender))
        {
            sender.Dispose();
        }
    }
}