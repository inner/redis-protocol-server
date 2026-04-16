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

## 11. What Would Need To Change For A Full Refactor

This is the important part: a transport-only refactor is not enough.

Right now the system is string-based across almost the whole execution path:

- transport reads a full request into `string`
- parser splits that `string`
- command dispatch works from `string[] CommandParts`
- commands build responses as `string`
- commands send those `string` responses directly to sockets
- transactions queue raw RESP strings
- replication forwards raw RESP strings

So if you want the server to become truly more performant end to end, the
refactor has to move several layers together.

The most useful mental model is:

```text
Today:
bytes -> string -> split strings -> execute -> build response string -> bytes

Target:
bytes -> parsed command -> execute -> write RESP bytes directly
```

Below is what would need to change.

## 12. Step 1: Move The Transport Boundary From `string` To Buffered Bytes

### Current Shape

From the active path:

```csharp
// The transport fully materializes a request as a string.
var resp = client.GetStream().AsString();

// The receiver only knows how to accept a string payload.
await receiver.Receive(client.Client, resp, commandQueue, subscriptions, commandSource);
```

```csharp
public virtual async Task Receive(
    Socket socket,
    string resp,
    List<CommandQueueItem> commandQueue,
    List<string> subscriptions,
    CommandSource source)
{
    var respDataType = resp.GetRespDataType();
    await executor.Execute(socket, resp, commandQueue, subscriptions, this, source);
}
```

### Fully Refactored Shape

The receiver should stop accepting a prebuilt `string` and instead consume bytes
from a `PipeReader`.

```csharp
// Illustrative design: the parser/receiver now operates on the connection's input pipe.
public async Task ProcessConnectionAsync(
    Connection connection,
    List<CommandQueueItem> commandQueue,
    List<string> subscriptions,
    CommandSource source)
{
    while (true)
    {
        // Read whatever bytes are currently buffered for this connection.
        var result = await connection.Input.ReadAsync();
        var buffer = result.Buffer;

        // Parse as many full RESP commands as exist in the buffer.
        while (TryParseRespCommand(buffer, out var command, out var consumed))
        {
            buffer = buffer.Slice(consumed);
            await ExecuteParsedCommandAsync(connection, command, commandQueue, subscriptions, source);
        }

        // Preserve unconsumed bytes so partial frames survive until the next read.
        connection.Input.AdvanceTo(buffer.Start, buffer.End);

        if (result.IsCompleted)
        {
            break;
        }
    }
}
```

### Why This Change Matters

- The parser becomes frame-aware.
- Incomplete commands stay buffered.
- You stop forcing the whole request path through `string`.

## 13. Step 2: Replace `CommandDetails` With A Parsed Command Model

### Current Shape

From [Redis/Commands/Common/CommandDetails.cs](./Redis/Commands/Common/CommandDetails.cs):

```csharp
public class CommandDetails
{
    public required int CommandCount { get; init; }
    public required string[] CommandParts { get; init; }
    public required string Resp { get; init; }
    public required RespType RespType { get; init; }
    public required bool FromTransaction { get; set; }
}
```

And from [Redis/Common/StringExtensions.cs](./Redis/Common/StringExtensions.cs):

```csharp
public static CommandDetails BuildCommandDetails(this string resp)
{
    var commandParts = resp.Split(Constants.VerbatimNewLine)
        .Where(x => !string.IsNullOrEmpty(x))
        .ToArray();

    return new CommandDetails
    {
        CommandCount = int.Parse(commandParts[0].Replace("*", string.Empty)),
        CommandParts = commandParts,
        Resp = resp,
        RespType = commandParts[2].ToCommandType(),
        FromTransaction = false
    };
}
```

This model already assumes:

- the whole request exists as a string
- the request is split into strings eagerly
- the raw request string is preserved for replication and transactions

### Fully Refactored Shape

You want a parsed command object whose primary representation is not a raw RESP
string.

```csharp
// Illustrative design: parsed command built from buffered bytes.
public sealed class ParsedCommand
{
    // The command name is decoded once.
    public required RespType RespType { get; init; }

    // Arguments stay as UTF-8 slices or rented byte buffers.
    public required ReadOnlyMemory<byte>[] Arguments { get; init; }

    // Preserve encoded bytes only if you need to replay or propagate them.
    public required ReadOnlyMemory<byte> EncodedResp { get; init; }

    // Transaction bookkeeping still lives here.
    public bool FromTransaction { get; set; }

    // Decode only when a command actually needs a string view.
    public string GetArgumentAsString(int index) =>
        System.Text.Encoding.UTF8.GetString(Arguments[index].Span);
}
```

### Why This Change Matters

- Argument decoding becomes selective instead of universal.
- You can keep the encoded command bytes for replication.
- Transactions and replay stop depending on rebuilding raw strings.

## 14. Step 3: Replace String-Based Dispatch With Parsed Command Dispatch

### Current Shape

From [Redis/Executors/ArrayExecutor.cs](./Redis/Executors/ArrayExecutor.cs):

```csharp
var multiRespSplit = Regex.Split(resp, @"(\*\d+\\r\\n)")
    .Where(x => !string.IsNullOrWhiteSpace(x))
    .Select(x => x.TrimEnd())
    .ToList();

foreach (var commandDetails in respCommands.Select(respCommand => respCommand.BuildCommandDetails()))
{
    await receiver.ExecuteCommand(socket, commandDetails, commandQueue, subscriptions, source);
}
```

This means:

- regex split on the full string payload
- reconstruction of command substrings
- then another parse step into `CommandDetails`

### Fully Refactored Shape

This layer would ideally stop being regex-based entirely.

```csharp
// Illustrative design: a RESP parser produces ParsedCommand directly.
while (TryParseRespCommand(buffer, out var command, out var consumed))
{
    // Execute the parsed command immediately.
    await receiver.ExecuteCommand(connection, command, commandQueue, subscriptions, source);

    // Move forward only by the bytes that formed this command.
    buffer = buffer.Slice(consumed);
}
```

### Why This Change Matters

- No regex split on full request strings.
- No rebuild of intermediate RESP command strings.
- Parsing and dispatch become one streaming pipeline.

## 15. Step 4: Change Command APIs So Commands No Longer Touch Sockets Directly

### Current Shape

From [Redis/Commands/Common/Base.cs](./Redis/Commands/Common/Base.cs):

```csharp
protected abstract Task<string> ExecuteCore(CommandContext commandContext);
```

And commands commonly do this:

```csharp
var resp = RespBuilder.SimpleString("PONG");
commandContext.Socket.SendCommand(resp);
return Task.FromResult(resp);
```

This means command execution is coupled to:

- socket writes
- string response building
- response replay through returned strings

### Fully Refactored Shape

Commands should stop sending raw socket bytes themselves and stop returning
response strings.

Instead, commands should write through a response writer abstraction.

```csharp
// Illustrative design: commands write bytes through a writer owned by the connection.
public abstract class Base
{
    protected abstract ValueTask ExecuteCoreAsync(CommandContext commandContext);

    public async ValueTask ExecuteAsync(CommandContext commandContext)
    {
        if (!IsSupportedOnCurrentNodeRole())
        {
            await commandContext.Writer.WriteErrorAsync(
                $"Command '{commandContext.Command.RespType}' is not allowed on this node.");
            return;
        }

        if (TransactionEnabled(commandContext))
        {
            return;
        }

        await ExecuteCoreAsync(commandContext);
    }
}
```

And `CommandContext` would change from socket-centric to writer-centric:

```csharp
// Illustrative design.
public sealed class CommandContext
{
    public required Connection Connection { get; init; }
    public required RespWriter Writer { get; init; }
    public required ParsedCommand Command { get; init; }
    public required List<CommandQueueItem> CommandQueue { get; init; }
    public required List<string> Subscriptions { get; init; }
    public required CommandSource Source { get; init; }
}
```

### Why This Change Matters

- Commands stop caring about sockets.
- The output path can become byte-oriented and buffered.
- The execution contract becomes clearer: execute behavior, do not manage
  transport details.

## 16. Step 5: Rewrite `RespBuilder` Into A Byte Writer

### Current Shape

From [Redis/Common/RespBuilder.cs](./Redis/Common/RespBuilder.cs):

```csharp
public static string SimpleString(string value)
{
    return $"+{value}\r\n";
}

public static string Integer(long value)
{
    return $":{value}\r\n";
}
```

And then later:

```csharp
socket.Send(resp.Replace(Constants.VerbatimNewLine, Constants.NewLine).AsBytes());
```

So responses are:

- built as strings
- normalized as strings
- encoded as bytes later

### Fully Refactored Shape

The builder should become a writer over `IBufferWriter<byte>` or `PipeWriter`.

```csharp
// Illustrative design: byte-oriented RESP writer.
public sealed class RespWriter
{
    private readonly PipeWriter writer;

    public RespWriter(PipeWriter writer)
    {
        this.writer = writer;
    }

    public void WriteSimpleString(string value)
    {
        // Write RESP prefix directly.
        WriteAscii("+");

        // Encode only the payload once.
        WriteUtf8(value);

        // Write CRLF directly as bytes.
        WriteAscii("\r\n");
    }

    public void WriteInteger(long value)
    {
        WriteAscii(":");
        WriteAscii(value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        WriteAscii("\r\n");
    }

    public void WriteBulkString(string value)
    {
        WriteAscii("$");
        WriteAscii(value.Length.ToString(System.Globalization.CultureInfo.InvariantCulture));
        WriteAscii("\r\n");
        WriteUtf8(value);
        WriteAscii("\r\n");
    }

    public ValueTask FlushAsync() => writer.FlushAsync();

    private void WriteAscii(string value)
    {
        var bytes = System.Text.Encoding.ASCII.GetBytes(value);
        writer.Write(bytes);
    }

    private void WriteUtf8(string value)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(value);
        writer.Write(bytes);
    }
}
```

### Why This Change Matters

- Responses can be encoded directly into the connection output pipe.
- The command path no longer builds transient response strings by default.
- You can later optimize numeric and ASCII paths further if needed.

## 17. Step 6: Every Command Would Need To Change

This is the part you were asking about explicitly.

Because commands currently:

- read arguments from `string[] CommandParts`
- build response strings with `RespBuilder`
- call `Socket.SendCommand(...)`
- return `string`

almost every command would need mechanical but real changes.

### Example: `PING` Today

```csharp
protected override Task<string> ExecuteCore(CommandContext commandContext)
{
    if (commandContext.IsReplicationStream)
    {
        return Task.FromResult(string.Empty);
    }

    var resp = commandContext.Subscriptions.Count > 0
        ? RespBuilder.ArrayFromCommands("pong", string.Empty)
        : RespBuilder.SimpleString("PONG");

    commandContext.Socket.SendCommand(resp);
    return Task.FromResult(resp);
}
```

### Example: `PING` After A Full Refactor

```csharp
// Illustrative design.
protected override async ValueTask ExecuteCoreAsync(CommandContext commandContext)
{
    if (commandContext.IsReplicationStream)
    {
        // Replication stream PING should not emit a normal client response.
        return;
    }

    if (commandContext.Subscriptions.Count > 0)
    {
        // Write RESP array directly to the connection's output pipe.
        commandContext.Writer.WriteArrayHeader(2);
        commandContext.Writer.WriteBulkString("pong");
        commandContext.Writer.WriteBulkString(string.Empty);
    }
    else
    {
        commandContext.Writer.WriteSimpleString("PONG");
    }

    await commandContext.Writer.FlushAsync();
}
```

### Example: `SET` Today

The current `SET` implementation reads from `CommandParts` and returns string
responses.

Conceptually, it looks like this:

```csharp
// Today's pattern.
var key = commandContext.CommandDetails.CommandParts[4];
var value = commandContext.CommandDetails.CommandParts[6];

DataCache.Set(key, value, expiry);

SendIfNotFromTransaction(commandContext, RespBuilder.SimpleString("OK"));
return Task.FromResult(RespBuilder.SimpleString("OK"));
```

### Example: `SET` After A Full Refactor

```csharp
// Illustrative design.
protected override ValueTask ExecuteCoreAsync(CommandContext commandContext)
{
    // Decode only the arguments this command actually needs.
    var key = commandContext.Command.GetArgumentAsString(0);
    var value = commandContext.Command.GetArgumentAsString(1);

    DataCache.Set(key, value, expiry: null);

    if (!commandContext.Command.FromTransaction)
    {
        commandContext.Writer.WriteSimpleString("OK");
        return commandContext.Writer.FlushAsync();
    }

    return ValueTask.CompletedTask;
}
```

### What This Means In Practice

Nearly every command file would need to be updated because:

- `CommandDetails` becomes `ParsedCommand`
- `Socket` send calls disappear
- `Task<string>` becomes `ValueTask` or `Task`
- `RespBuilder` string-returning usage disappears

The changes are repetitive, but not conceptually hard once the core abstractions
are in place.

## 18. Step 7: Transactions Must Stop Storing Raw RESP Strings

### Current Shape

From [Redis/Commands/Common/Base.cs](./Redis/Commands/Common/Base.cs):

```csharp
var commandString = string.Join(
    Constants.VerbatimNewLine, commandContext.CommandDetails.CommandParts);

var commandType = commandContext.CommandDetails.CommandParts[2].ToCommandType();

commandContext.CommandQueue.Add(
    new CommandQueueItem { RespType = commandType, Resp = commandString });
```

And from [Redis/Commands/Common/CommandQueueItem.cs](./Redis/Commands/Common/CommandQueueItem.cs):

```csharp
public class CommandQueueItem
{
    public RespType RespType { get; init; }
    public required string Resp { get; init; }
}
```

This means transactions replay by storing raw RESP strings and reparsing them
later.

### Fully Refactored Shape

Transactions should queue parsed commands or encoded bytes, not reconstructed
strings.

```csharp
// Illustrative design.
public sealed class CommandQueueItem
{
    public required ParsedCommand Command { get; init; }
}
```

Then queuing becomes:

```csharp
commandContext.CommandQueue.Add(new CommandQueueItem
{
    // Keep the parsed command object or a stable encoded copy.
    Command = commandContext.Command
});
```

And `EXEC` would stop reparsing strings:

```csharp
foreach (var queued in commandContext.CommandQueue)
{
    queued.Command.FromTransaction = true;
    await commandContext.Receiver.ExecuteCommand(
        commandContext.Connection,
        queued.Command,
        [],
        [],
        commandContext.Source);
}
```

### Why This Change Matters

- No string rebuild during queueing.
- No reparse on `EXEC`.
- Transaction replay becomes closer to "re-execute parsed command".

## 19. Step 8: Replication Must Stop Propagating Strings

### Current Shape

From [Redis/Receivers/ReceiverExtensions.cs](./Redis/Receivers/ReceiverExtensions.cs):

```csharp
await ServerRuntimeContext.ExecuteOnReplicas(commandDetails.Resp);
```

From [Redis/ServerInfo.cs](./Redis/ServerInfo.cs):

```csharp
public static async Task ExecuteOnReplicas(string resp)
{
    var tasks = connectedReplicas
        .Select(replica =>
            Task.Run(() =>
                replica.Value.SendCommand(resp)));

    await Task.WhenAll(tasks);
}
```

This means propagation is also string-based:

- preserve or rebuild RESP string
- send string to replicas

### Fully Refactored Shape

Replication should forward encoded bytes, ideally without rebuilding them.

```csharp
// Illustrative design.
public static async Task ExecuteOnReplicasAsync(ReadOnlyMemory<byte> encodedResp)
{
    var tasks = ServerInfo.ServerRuntimeContext.Replicas
        .Where(x => x.Value.Connected)
        .Select(async replica =>
        {
            // Each replica connection owns an output writer or async sender.
            await replica.Value.Output.WriteAsync(encodedResp);
            await replica.Value.Output.FlushAsync();
        });

    await Task.WhenAll(tasks);
}
```

And the command execution bridge would propagate:

```csharp
if (ShouldReplicateCommand(command, parsedCommand))
{
    // Reuse the encoded command bytes that were originally parsed.
    await ServerRuntimeContext.ExecuteOnReplicasAsync(parsedCommand.EncodedResp);
}
```

### Why This Change Matters

- No rebuild of RESP string for propagation.
- Replica propagation can reuse already-encoded command bytes.
- Replication path becomes consistent with the rest of the byte-oriented design.

## 20. Step 9: Error Handling Must Also Use The Writer

### Current Shape

From [Redis/Receivers/ReceiverBase.cs](./Redis/Receivers/ReceiverBase.cs):

```csharp
catch (Exception ex)
{
    socket.SendCommand(RespBuilder.Error(ex.Message));
}
```

### Fully Refactored Shape

Error responses should be written through the same byte-oriented response path:

```csharp
catch (Exception ex)
{
    connection.Writer.WriteError(ex.Message);
    await connection.Writer.FlushAsync();
}
```

### Why This Change Matters

- One output path instead of mixed output styles.
- Error handling benefits from the same buffering and transport abstraction.

## 21. Files That Would Be Directly Affected

If you wanted to do this fully rather than partially, the main impact areas
would be:

- `Redis/Nodes/NodeBase.cs`
  because connection handling must switch from `NetworkStream` string reads to
  buffered async transport
- `Redis/Common/NetworkExtensions.cs`
  because most of the current string-based read/write helpers would either
  disappear or shrink to handshake-specific code
- `Redis/Receivers/ReceiverBase.cs`
  because it currently accepts `string resp`
- `Redis/Executors/ArrayExecutor.cs`
  because regex/string splitting should be replaced by a real incremental parser
- `Redis/Common/StringExtensions.cs`
  because `BuildCommandDetails(string resp)` is part of the string pipeline
- `Redis/Commands/Common/CommandDetails.cs`
  because the parsed command model needs to change
- `Redis/Commands/Common/CommandQueueItem.cs`
  because transactions should not queue raw strings
- `Redis/Commands/Common/Base.cs`
  because commands should stop returning response strings and stop sending raw
  socket writes directly
- almost every file under `Redis/Commands`
  because commands currently read `CommandParts`, build string responses, and
  write through `Socket.SendCommand(...)`
- `Redis/Common/RespBuilder.cs`
  because it would need to become a byte-oriented writer
- `Redis/Receivers/ReceiverExtensions.cs`
  because dispatch and propagation currently assume `CommandDetails.Resp`
- `Redis/ServerInfo.cs`
  because replica propagation currently sends strings

## 22. What A "Fully More Performant" Version Really Means

If you truly wanted the refactor to be complete, the goal would not be:

- "use `SocketAsyncEventArgs` somewhere"

The goal would be:

- async transport
- byte-oriented parser
- parsed-command execution model
- byte-oriented response writer
- transaction queue that stores parsed or encoded commands
- replication path that forwards encoded bytes

That is the real end-to-end change.

## 23. Best Summary

The hard truth is:

- swapping only the transport layer would help
- but it would leave a lot of performance on the table
- because the rest of the server is still string-centric

A real refactor would need to move the whole request/response pipeline from:

```text
NetworkStream -> string -> split -> command parts -> response string -> socket send
```

to:

```text
SocketAsyncEventArgs -> PipeReader -> parsed command -> command execution -> PipeWriter
```

That is why "fully more performant" is not one change. It is a pipeline-wide
architectural shift.
