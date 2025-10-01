using System.Net.Sockets;
using System.Threading.Tasks.Sources;

namespace Redis.Sockets;

public abstract class AwaitableEventArgs()
    : SocketAsyncEventArgs(unsafeSuppressExecutionContextFlow: true), IValueTaskSource<int>
{
    private ManualResetValueTaskSourceCore<int> source;

    protected override void OnCompleted(SocketAsyncEventArgs args)
    {
        if (SocketError != SocketError.Success)
        {
            source.SetException(new SocketException((int)SocketError));
        }

        source.SetResult(BytesTransferred);
    }

    public int GetResult(short token)
    {
        var result = source.GetResult(token);
        source.Reset();
        return result;
    }

    public ValueTaskSourceStatus GetStatus(short token)
    {
        return source.GetStatus(token);
    }

    public void OnCompleted(Action<object?> continuation, object? state, short token,
        ValueTaskSourceOnCompletedFlags flags)
    {
        source.OnCompleted(continuation, state, token, flags);
    }
}