using System.Net.Sockets;
using System.Text;
using Redis.Common;
using Redis.Receivers;

namespace Redis.Aof;

// ==============================================================================
// Append-only file persistence
// ==============================================================================

// Owns the on-disk AOF layout: <dir>/<appenddirname>/... Startup work happens
// in Initialize, which runs before the server accepts any connection so that
// clients never observe a half-prepared persistence directory.
public static class AofManager
{
    private static AofOptions Options => ServerInfo.ServerRuntimeContext.Aof;

    public static bool Enabled =>
        string.Equals(Options.AppendOnly, "yes", StringComparison.OrdinalIgnoreCase);

    public static string DirectoryPath =>
        Path.Combine(ServerInfo.ServerRuntimeContext.DataDir, Options.AppendDirName);

    // Redis 7 multi-part AOF naming: the first incremental file is
    // <appendfilename>.1.incr.aof.
    private static string DefaultIncrFileName => $"{Options.AppendFilename}.1.incr.aof";

    public static string ManifestFilePath =>
        Path.Combine(DirectoryPath, $"{Options.AppendFilename}.manifest");

    // The incremental file currently receiving writes, as named by the manifest.
    private static FileStream? _incrFile;
    private static readonly object WriteLock = new();
    private static Timer? _everySecFsync;

    // ==========================================================================
    // Startup
    // ==========================================================================

    public static void Initialize()
    {
        if (!Enabled)
        {
            return;
        }

        // CreateDirectory is a no-op when the directory already exists.
        Directory.CreateDirectory(DirectoryPath);

        // The manifest is the source of truth for which file is active. A
        // fresh setup gets a manifest naming the default first incremental
        // file; an existing one is honoured as-is, whatever names it uses.
        if (!File.Exists(ManifestFilePath))
        {
            File.WriteAllText(ManifestFilePath, $"file {DefaultIncrFileName} seq 1 type i\n");
        }

        var incrFileName = ReadActiveIncrFileName();
        var incrFilePath = Path.Combine(DirectoryPath, incrFileName);

        // Replay runs before the write stream is opened, so Append is a no-op
        // during replay and the replayed commands are not logged a second time.
        if (File.Exists(incrFilePath))
        {
            Replay(incrFilePath);
        }

        // Open for appending: creates the file if missing and never truncates
        // the persisted write history.
        _incrFile = new FileStream(
            incrFilePath,
            FileMode.Append,
            FileAccess.Write,
            FileShare.ReadWrite);

        // With everysec, writes reach the OS immediately but are only forced
        // to the disk once per second, trading up to 1s of data for latency.
        if (string.Equals(Options.AppendFsync, "everysec", StringComparison.OrdinalIgnoreCase))
        {
            _everySecFsync = new Timer(_ => Fsync(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }
    }

    // Manifest lines look like: file <name> seq <n> type <b|h|i>
    // The last `type i` entry is the incremental file to append to.
    private static string ReadActiveIncrFileName()
    {
        string? active = null;

        foreach (var line in File.ReadLines(ManifestFilePath))
        {
            var tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string? name = null;
            string? type = null;

            for (var i = 0; i + 1 < tokens.Length; i += 2)
            {
                switch (tokens[i])
                {
                    case "file": name = tokens[i + 1]; break;
                    case "type": type = tokens[i + 1]; break;
                }
            }

            if (type == "i" && name != null)
            {
                active = name;
            }
        }

        return active ?? throw new InvalidOperationException(
            $"AOF manifest '{ManifestFilePath}' has no incremental (type i) entry.");
    }

    // ==========================================================================
    // Replay on startup
    // ==========================================================================

    // Rebuilds the keyspace by executing every logged command through the
    // normal command pipeline, so replay semantics never drift from live ones.
    // TODO: SET ... PX is logged with a relative TTL, so a replayed key lives
    // longer than intended; Redis logs absolute PXAT instead.
    private static void Replay(string path)
    {
        var receiver = new ReceiverBase();

        // Commands reply on their socket. During replay there is no client, so
        // an unconnected socket stands in: replies via SendIfNotFromTransaction
        // are suppressed (FromTransaction), and the few commands that reply
        // directly throw on Send *after* applying their change, which is
        // caught below.
        using var noClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        foreach (var resp in ParseCommands(File.ReadAllBytes(path)))
        {
            try
            {
                var commandDetails = resp.BuildCommandDetails();
                commandDetails.FromTransaction = true;

                receiver
                    .ExecuteCommand(noClient, commandDetails, [], [], CommandSource.Client)
                    .GetAwaiter()
                    .GetResult();
            }
            catch (Exception ex) when (ex is SocketException or ObjectDisposedException)
            {
                // Reply could not be delivered; the command itself has run.
            }
        }
    }

    // Splits the file into RESP arrays of bulk strings. Lengths are byte
    // counts, so framing is done on bytes before decoding each command. The
    // result uses the escaped "\r\n" form the rest of the pipeline expects.
    // A truncated trailing command (e.g. after a crash mid-write) is ignored.
    private static IEnumerable<string> ParseCommands(byte[] data)
    {
        var position = 0;

        while (position < data.Length)
        {
            var start = position;

            if (data[position] != (byte)'*' || !TryReadLine(data, ref position, out var header) ||
                !int.TryParse(header.AsSpan(1), out var elementCount))
            {
                yield break;
            }

            for (var i = 0; i < elementCount; i++)
            {
                if (position >= data.Length || data[position] != (byte)'$' ||
                    !TryReadLine(data, ref position, out var lengthLine) ||
                    !int.TryParse(lengthLine.AsSpan(1), out var length) ||
                    position + length + 2 > data.Length)
                {
                    yield break;
                }

                // Skip the payload and its trailing \r\n.
                position += length + 2;
            }

            yield return Encoding.UTF8.GetString(data, start, position - start)
                .Replace(Constants.NewLine, Constants.VerbatimNewLine);
        }
    }

    private static bool TryReadLine(byte[] data, ref int position, out string line)
    {
        for (var i = position; i + 1 < data.Length; i++)
        {
            if (data[i] == (byte)'\r' && data[i + 1] == (byte)'\n')
            {
                line = Encoding.ASCII.GetString(data, position, i - position);
                position = i + 2;
                return true;
            }
        }

        line = string.Empty;
        return false;
    }

    // ==========================================================================
    // Logging write commands
    // ==========================================================================

    // Transaction control and commands whose raw form must not be replayed
    // verbatim are not logged.
    // TODO: Redis logs a served BLPOP as the equivalent LPOP; BLPOP is skipped
    // here so a replay can never block.
    private static readonly HashSet<RespType> NotLogged =
    [
        RespType.Multi, RespType.Exec, RespType.Discard, RespType.Publish, RespType.Blpop
    ];

    public static bool ShouldLog(RespType commandType, bool isWriteCommand) =>
        Enabled && isWriteCommand && !NotLogged.Contains(commandType);

    // Appends the command exactly as received. Called before the command
    // replies, so with appendfsync=always the client only sees a response
    // once the command is durable.
    public static void Append(string resp)
    {
        if (_incrFile == null)
        {
            return;
        }

        // The network layer hands commands over with escaped "\r\n" text;
        // the file must hold the real RESP bytes.
        var bytes = resp.Replace(Constants.VerbatimNewLine, Constants.NewLine).AsBytes();

        lock (WriteLock)
        {
            _incrFile.Write(bytes);

            // always: durable before the reply. everysec / no: hand the data
            // to the OS; the timer (everysec) or the OS (no) syncs it later.
            _incrFile.Flush(flushToDisk: string.Equals(
                Options.AppendFsync, "always", StringComparison.OrdinalIgnoreCase));
        }
    }

    private static void Fsync()
    {
        lock (WriteLock)
        {
            _incrFile?.Flush(flushToDisk: true);
        }
    }
}
