# Transport Performance Notes

This document explains why the current transport path is relatively expensive
and why the `Redis/Sockets` transport foundation can scale better.

The goal is not to say the current code is "bad". The current path is simple,
understandable, and good enough for learning and challenge progress.

The goal is to show:

- what the current implementation costs
- what the `Sockets` design improves
- what each improvement means in concrete code

Important scope note:

- The current active server path is the `TcpListener` / `TcpClient` /
  `NetworkStream` / string pipeline.
- The `Redis/Sockets` code is a more advanced transport foundation, but it is
  not yet wired into the active server path.
- Some "better way" examples below are taken directly from `Redis/Sockets`,
  while a few parser examples are illustrative next steps that this transport
  would enable.

## Current Active Path

The active server path today is mainly:

- [Redis/Nodes/NodeBase.cs](./Redis/Nodes/NodeBase.cs)
- [Redis/Common/NetworkExtensions.cs](./Redis/Common/NetworkExtensions.cs)

The important parts are:

```csharp
// NodeBase.Start()
// Accept a TCP client and run one connection handler per accepted client.
var server = new TcpListener(localAddress, port);
server.Start();

while (true)
{
    var client = server.AcceptTcpClient();
    _ = Task.Run(() => HandleConnection(client));
}
```

```csharp
// NodeBase.HandleConnection()
// Read the whole request as a string, then dispatch it.
while (true)
{
    var resp = client.GetStream()
        .AsString();

    if (string.IsNullOrEmpty(resp))
    {
        client.Client.SendCommand(RespBuilder.Null());
        continue;
    }

    await receiver.Receive(client.Client, resp, commandQueue, subscriptions, commandSource);
}
```

```csharp
// NetworkExtensions.AsString()
// Read bytes from the network stream, copy them into a MemoryStream,
// then turn the final byte[] into a string.
public static string AsString(this NetworkStream stream)
{
    using var memoryStream = new MemoryStream();
    var buffer = new byte[1024];
    int bytesRead;

    while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
    {
        memoryStream.Write(buffer, 0, bytesRead);
        if (bytesRead < buffer.Length)
        {
            break;
        }
    }

    return memoryStream.ToArray()
        .AsString()
        .Replace(Constants.NewLine, Constants.VerbatimNewLine);
}
```

```csharp
// NetworkExtensions.SendCommand()
// Convert the response string to bytes and write it to the socket.
public static void SendCommand(this Socket socket, string resp)
{
    socket.Send(resp.Replace(Constants.VerbatimNewLine, Constants.NewLine).AsBytes());
}
```

## 1. Blocking Connection Model vs Async Socket I/O

### Current Way

```csharp
// Accept a client synchronously.
var client = server.AcceptTcpClient();

// Push the connection handler onto the thread pool.
_ = Task.Run(() => HandleConnection(client));
```

```csharp
// Inside the handler, read synchronously from NetworkStream.
// If the client is slow or idle, this read blocks the executing thread.
var resp = client.GetStream().AsString();
```

### Why This Is Expensive

- `AcceptTcpClient()` is synchronous.
- `stream.Read(...)` inside `AsString()` is synchronous.
- A slow connection can keep a thread occupied while the server is waiting for
  network I/O.
- Under many concurrent connections, this drifts toward a
  thread-per-blocked-connection shape.

That is workable for small scale, but it is not the model you want if you care
about high connection counts or slow clients.

### Better Way

From [Redis/Sockets/Receiver.cs](./Redis/Sockets/Receiver.cs):

```csharp
public ValueTask<int> ReceiveAsync(Socket socket, Memory<byte> memory)
{
    // Point SocketAsyncEventArgs at a caller-provided buffer.
    SetBuffer(memory);

    // Ask the OS to perform async socket receive.
    if (socket.ReceiveAsync(this))
    {
        // If the operation is pending, return a ValueTask that completes later.
        return new ValueTask<int>(this, token++);
    }

    // If the operation completed synchronously, return immediately.
    var transferred = BytesTransferred;
    var err = SocketError;

    return err == SocketError.Success
        ? new ValueTask<int>(transferred)
        : ValueTask.FromException<int>(new SocketException((int)err));
}
```

From [Redis/Sockets/Connection.cs](./Redis/Sockets/Connection.cs):

```csharp
private async Task ReceiveLoop()
{
    while (true)
    {
        // Ask the PipeWriter for writable memory.
        var buff = applicationPipe.Writer.GetMemory(MinBuffSize);

        // Perform async socket receive directly into that memory.
        var bytes = await receiver.ReceiveAsync(socket, buff);

        if (bytes == 0)
        {
            break;
        }

        // Tell the pipe how many bytes were filled.
        applicationPipe.Writer.Advance(bytes);

        // Publish the bytes to downstream consumers.
        var result = await applicationPipe.Writer.FlushAsync();
        if (result.IsCanceled || result.IsCompleted)
        {
            break;
        }
    }
}
```

### Why This Is Better

- The OS completes the socket operation asynchronously.
- Threads are not blocked waiting on slow network reads.
- The connection loop is driven by completion, not by a synchronous read call.
- The receive path writes directly into reusable pipe memory.

## 2. Repeated Allocation and Copying in `AsString()`

### Current Way

From [Redis/Common/NetworkExtensions.cs](./Redis/Common/NetworkExtensions.cs):

```csharp
public static string AsString(this NetworkStream stream)
{
    // Allocate a MemoryStream for every read.
    using var memoryStream = new MemoryStream();

    // Allocate a temporary buffer for every read call into AsString().
    var buffer = new byte[1024];
    int bytesRead;

    while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
    {
        // Copy bytes from the temp buffer into the MemoryStream's internal buffer.
        memoryStream.Write(buffer, 0, bytesRead);

        if (bytesRead < buffer.Length)
        {
            break;
        }
    }

    return memoryStream.ToArray()
        // Allocate a new byte[] via ToArray().
        // Convert the byte[] to a string.
        .AsString()

        // Allocate another string after replacing line endings.
        .Replace(Constants.NewLine, Constants.VerbatimNewLine);
}
```

### Why This Is Expensive

A single request can involve all of these allocations or copies:

- `MemoryStream`
- temporary `byte[]`
- copy from temp `byte[]` into `MemoryStream`
- `MemoryStream.ToArray()` creates another `byte[]`
- UTF-8 decode creates a `string`
- `Replace(...)` creates another `string`

That means extra:

- allocation pressure
- copies between buffers
- GC work

### Better Way

From [Redis/Sockets/Connection.cs](./Redis/Sockets/Connection.cs):

```csharp
var buff = applicationPipe.Writer.GetMemory(MinBuffSize);
var bytes = await receiver.ReceiveAsync(socket, buff);

if (bytes == 0)
{
    break;
}

// The transport writes straight into memory provided by the pipe.
// No MemoryStream is created here.
applicationPipe.Writer.Advance(bytes);
await applicationPipe.Writer.FlushAsync();
```

### Why This Is Better

- No per-read `MemoryStream`.
- No immediate `ToArray()` copy.
- No forced conversion of all received bytes into a `string`.
- Bytes stay in a buffer-oriented representation longer.

That does not automatically make the whole server fast, but it removes a large
class of avoidable transport allocations.

## 3. Converting to String Too Early

### Current Way

```csharp
// The request becomes a string at the transport boundary.
var resp = client.GetStream().AsString();

// Downstream logic now works on the whole materialized string.
await receiver.Receive(client.Client, resp, commandQueue, subscriptions, commandSource);
```

This means the system:

- reads bytes
- materializes the whole payload as a string
- then parses RESP from that string

That is simple, but it means transport and parsing are tightly coupled to a
full-string representation.

### Why This Is Expensive

- You cannot parse incrementally as bytes arrive.
- You cannot easily stop after one frame and leave the remainder buffered.
- You force UTF-8 decoding before you even know whether a full command frame is
  present.

### Better Way

The current `Sockets` transport already exposes the right shape for a better
parser:

From [Redis/Sockets/Connection.cs](./Redis/Sockets/Connection.cs):

```csharp
public PipeReader Input { get; }
public PipeWriter Output { get; }
```

That enables a byte-first parser. A future RESP parser on top of `PipeReader`
could look like this:

```csharp
// Illustrative example: not wired into the current server yet.
public async Task ReadCommandsAsync(PipeReader input)
{
    while (true)
    {
        // Read whatever bytes are currently available.
        var result = await input.ReadAsync();
        var buffer = result.Buffer;

        // Try to parse one full RESP command from the available bytes.
        if (TryParseRespCommand(buffer, out var command, out var consumed))
        {
            // Advance only past the bytes that were actually parsed.
            input.AdvanceTo(consumed, buffer.End);

            // Process the parsed command without materializing unrelated bytes.
            await HandleCommand(command);
            continue;
        }

        // Not enough bytes for a full frame yet.
        // Keep the unconsumed bytes in the pipe and wait for more.
        input.AdvanceTo(buffer.Start, buffer.End);

        if (result.IsCompleted)
        {
            break;
        }
    }
}
```

### Why This Is Better

- The parser can operate directly on bytes.
- Incomplete frames stay buffered without forcing a whole-string decode.
- Multiple commands in one receive can be parsed one by one.
- Leftover bytes remain in the pipe for the next read.

This is one of the biggest architectural wins of a pipeline-oriented transport.

## 4. Response Allocation on Every Send

### Current Way

From [Redis/Common/NetworkExtensions.cs](./Redis/Common/NetworkExtensions.cs):

```csharp
public static void SendCommand(this Socket socket, string resp)
{
    // Replace creates a new string.
    // AsBytes creates a new byte[].
    // Then the bytes are sent.
    socket.Send(resp.Replace(Constants.VerbatimNewLine, Constants.NewLine).AsBytes());
}
```

### Why This Is Expensive

For every response:

- response normalization creates a new string
- UTF-8 encoding creates a new byte array
- the socket send uses that newly created byte array

That is simple and perfectly fine for a learning implementation, but it creates
avoidable churn on the hot path.

### Better Way

From [Redis/Sockets/Sender.cs](./Redis/Sockets/Sender.cs):

```csharp
public ValueTask<int> SendAsync(Socket socket, in ReadOnlySequence<byte> data)
{
    if (data.IsSingleSegment)
    {
        // If the response is already one contiguous segment,
        // send it directly.
        return SendAsync(socket, data.First);
    }

    // If the response spans multiple segments,
    // gather them into BufferList instead of forcing a merge copy.
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
```

### Why This Is Better

- The sender can work on byte buffers directly.
- Multi-segment responses do not have to be flattened into one merged array
  before sending.
- The send operation itself is async and reusable.

The full benefit appears when the response path also stops building full strings
first and instead writes RESP directly as bytes.

## 5. Short-Read Heuristic vs Frame-Aware Parsing

### Current Way

```csharp
while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
{
    memoryStream.Write(buffer, 0, bytesRead);

    // Stop reading when a short read happens.
    // This assumes "short read means full message",
    // which is not a protocol-aware guarantee.
    if (bytesRead < buffer.Length)
    {
        break;
    }
}
```

### Why This Is Fragile

A TCP read boundary is not a message boundary.

Problems with this heuristic:

- one RESP command may arrive split across multiple reads
- multiple RESP commands may arrive in one read
- a short read does not necessarily mean "message completed"
- a full-sized read does not necessarily mean "more data belongs to the same command"

The code works often enough for the current exercise path, but it does not
actually understand RESP framing at the transport level.

### Better Way

With `PipeReader`, the transport can keep bytes around until the parser confirms
that a full frame exists:

```csharp
// Illustrative example: frame-aware parsing loop over PipeReader.
while (true)
{
    var result = await input.ReadAsync();
    var buffer = result.Buffer;

    if (TryParseRespCommand(buffer, out var command, out var consumed))
    {
        // Consume exactly one complete frame.
        input.AdvanceTo(consumed, buffer.End);
        await HandleCommand(command);
    }
    else
    {
        // Do not throw data away.
        // Keep the partial frame buffered until more bytes arrive.
        input.AdvanceTo(buffer.Start, buffer.End);
    }

    if (result.IsCompleted)
    {
        break;
    }
}
```

### Why This Is Better

- Parsing is based on RESP framing, not on read size.
- Partial commands remain buffered correctly.
- Multiple commands in one buffer can be processed correctly.
- Backpressure and consumption are explicit.

## 6. `SocketAsyncEventArgs` Avoids Per-Operation Async Overhead

### Current Way

The current active path mostly uses:

```csharp
// Synchronous read.
var bytesRead = stream.Read(buffer, 0, buffer.Length);

// Synchronous send.
socket.Send(bytes);
```

That is simple, but it does not use the lower-overhead reusable async socket
machinery that .NET exposes for high-throughput networking.

### Better Way

From [Redis/Sockets/AwaitableEventArgs.cs](./Redis/Sockets/AwaitableEventArgs.cs):

```csharp
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
}
```

### Why This Is Better

- `SocketAsyncEventArgs` is reusable.
- `IValueTaskSource<int>` allows awaiting completion without allocating a new
  `Task<int>` every time.
- This is a standard high-performance pattern in .NET networking.

This matters most when a server is doing many sends and receives per second.

## 7. Why `unsafeSuppressExecutionContextFlow: true` Helps

### Current Way

The current active path does not try to optimize execution-context flow at all.

That is not wrong. It is just the default.

### Better Way

From [Redis/Sockets/AwaitableEventArgs.cs](./Redis/Sockets/AwaitableEventArgs.cs):

```csharp
// SocketAsyncEventArgs normally participates in execution context flow.
// Here the transport explicitly opts out for lower overhead.
public abstract class AwaitableEventArgs()
    : SocketAsyncEventArgs(unsafeSuppressExecutionContextFlow: true), IValueTaskSource<int>
{
}
```

### Why This Is Better

Execution context flow can carry things like:

- async locals
- security context
- ambient state

That machinery has overhead. On a hot networking path, suppressing that flow
can reduce per-operation cost.

The important word is `unsafe`:

- this is a deliberate performance tradeoff
- it is appropriate only when you understand that the transport should not rely
  on ambient async context

## 8. Reusing Sender Objects with `SenderPool`

### Current Way

The active path does not have a reusable transport sender abstraction.

Each send effectively does the work fresh:

```csharp
// Build a string.
// Normalize line endings.
// Encode to bytes.
// Send with the socket.
socket.Send(resp.Replace(Constants.VerbatimNewLine, Constants.NewLine).AsBytes());
```

There is no sender reuse or send-object pooling here.

### Better Way

From [Redis/Sockets/SenderPool.cs](./Redis/Sockets/SenderPool.cs):

```csharp
public Sender Rent()
{
    if (senders.TryDequeue(out var sender))
    {
        Interlocked.Decrement(ref count);

        // Reset a previously used Sender and reuse it.
        sender.Reset();
        return sender;
    }

    // Only allocate a new Sender when the pool is empty.
    return new Sender();
}
```

```csharp
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
```

From [Redis/Sockets/Connection.cs](./Redis/Sockets/Connection.cs):

```csharp
sender = senderPool.Rent();
await sender.SendAsync(socket, result.Buffer);
senderPool.Return(sender);
sender = null;
```

### Why This Is Better

- Reuses transport send helpers instead of allocating them repeatedly.
- Keeps the hot path more stable under load.
- Works well with reusable async socket event args.

Pooling is not always worth it for every object, but on hot transport
components it can make a real difference.

## 9. Why Pipelines Fit Protocol Parsing Better

### Current Way

The current transport hands a fully materialized string to the parser:

```csharp
var resp = client.GetStream().AsString();
await receiver.Receive(client.Client, resp, commandQueue, subscriptions, commandSource);
```

The parser works after the transport has already:

- decided where reads stop
- copied bytes into a memory stream
- converted bytes into a string

### Better Way

From [Redis/Sockets/Connection.cs](./Redis/Sockets/Connection.cs):

```csharp
private readonly Pipe transportPipe;
private readonly Pipe applicationPipe;

public PipeWriter Output { get; }
public PipeReader Input { get; }
```

This model cleanly separates:

- socket transport
- buffering
- parsing
- application logic

An incremental RESP parser can read from `PipeReader` and consume exactly the
bytes it understands, leaving the rest buffered.

### Why This Is Better

- Better handling of partial frames
- Better handling of multiple frames in one read
- Less copying
- Cleaner backpressure behavior
- Better separation between transport and parser

`System.IO.Pipelines` does not magically make code fast, but it provides a much
better shape for building high-throughput protocol servers.

## 10. Separate Send and Receive Loops

### Current Way

The current path is conceptually request/response from one connection loop:

```csharp
while (true)
{
    var resp = client.GetStream().AsString();
    await receiver.Receive(client.Client, resp, commandQueue, subscriptions, commandSource);
}
```

That is easy to follow, but transport concerns are mixed together in one place.

### Better Way

From [Redis/Sockets/Connection.cs](./Redis/Sockets/Connection.cs):

```csharp
public void Start()
{
    sendTask = SendLoop();
    receiveTask = ReceiveLoop();
}
```

```csharp
private async Task SendLoop()
{
    while (true)
    {
        var result = await transportPipe.Reader.ReadAsync();
        var buff = result.Buffer;

        if (!buff.IsEmpty)
        {
            sender = senderPool.Rent();
            await sender.SendAsync(socket, result.Buffer);
            senderPool.Return(sender);
            sender = null;
        }

        transportPipe.Reader.AdvanceTo(buff.End);
    }
}
```

```csharp
private async Task ReceiveLoop()
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
        await applicationPipe.Writer.FlushAsync();
    }
}
```

### Why This Is Better

- Sending and receiving are independent.
- The connection can be naturally full-duplex.
- Backpressure can be handled separately on ingress and egress.
- The transport becomes easier to reason about in terms of data flow.

This is a stronger foundation for a Redis-like server, where reads and writes do
not always line up one-to-one.

## Bottom Line

The current path is expensive mainly because it is:

- synchronous
- string-first
- allocation-heavy
- not frame-aware at the transport boundary

The `Redis/Sockets` design is better mainly because it is:

- async
- buffer-first
- reusable
- pipeline-friendly
- a better foundation for incremental RESP parsing

The biggest performance story is not "replace `TcpListener` because it is bad".
The bigger story is:

- stop blocking threads on network I/O
- stop materializing whole strings too early
- stop copying bytes more than necessary
- parse frames from buffered bytes instead of read-size heuristics

## Best Mental Model

Think of the difference like this:

Current path:

```text
socket -> NetworkStream -> MemoryStream -> byte[] -> string -> RESP parser
```

Better path:

```text
socket -> async receive -> PipeReader/PipeWriter -> incremental RESP parser
```

That is why the `Sockets` folder is a stronger long-term transport design.
