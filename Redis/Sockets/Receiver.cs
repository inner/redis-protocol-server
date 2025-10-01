using System.Net.Sockets;

namespace Redis.Sockets;

public class Receiver : AwaitableEventArgs
{
    private short token;

    public ValueTask<int> ReceiveAsync(Socket socket, Memory<byte> memory)
    {
        SetBuffer(memory);
        if (socket.ReceiveAsync(this))
        {
            return new ValueTask<int>(this, token++);
        }

        var transferred = BytesTransferred;
        var err = SocketError;
        
        return err == SocketError.Success
            ? new ValueTask<int>(transferred)
            : ValueTask.FromException<int>(new SocketException((int)err));
    }
}