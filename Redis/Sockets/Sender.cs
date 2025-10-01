using System.Buffers;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace Redis.Sockets;

public class Sender : AwaitableEventArgs
{
    private short token;
    private List<ArraySegment<byte>>? buffers;

    public ValueTask<int> SendAsync(Socket socket, in ReadOnlyMemory<byte> data)
    {
        SetBuffer(MemoryMarshal.AsMemory(data));
        if (socket.SendAsync(this))
        {
            return new ValueTask<int>(this, token++);
        }

        var transferred = BytesTransferred;
        var err = SocketError;
        
        return err == SocketError.Success
            ? new ValueTask<int>(transferred)
            : ValueTask.FromException<int>(new SocketException((int)err));
    }

    public ValueTask<int> SendAsync(Socket socket, in ReadOnlySequence<byte> data)
    {
        if (data.IsSingleSegment)
        {
            return SendAsync(socket, data.First);
        }

        buffers ??= new List<ArraySegment<byte>>();
        foreach (var buff in data)
        {
            if (!MemoryMarshal.TryGetArray(buff, out var array))
            {
                throw new InvalidOperationException("Buffer is not backed by an array.");
            }

            buffers.Add(array);
        }

        BufferList = buffers;

        if (socket.SendAsync(this))
        {
            return new ValueTask<int>(this, token++);
        }

        var transferred = BytesTransferred;
        var err = SocketError;
        return err == SocketError.Success
            ? new ValueTask<int>(transferred)
            : ValueTask.FromException<int>(new SocketException((int)err));
    }

    public void Reset()
    {
        if (BufferList != null)
        {
            BufferList = null;

            buffers?.Clear();
        }
        else
        {
            SetBuffer(null, 0, 0);
        }
    }
}