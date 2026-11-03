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
    public static string IncrFileName => $"{Options.AppendFilename}.1.incr.aof";

    public static string IncrFilePath => Path.Combine(DirectoryPath, IncrFileName);

    public static string ManifestFilePath =>
        Path.Combine(DirectoryPath, $"{Options.AppendFilename}.manifest");

    public static void Initialize()
    {
        if (!Enabled)
        {
            return;
        }

        // CreateDirectory is a no-op when the directory already exists.
        Directory.CreateDirectory(DirectoryPath);

        // Create the incremental file if missing, but never truncate an
        // existing one: its contents are the persisted write history.
        using (new FileStream(IncrFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
        {
        }

        // The manifest lists the AOF parts in replay order. Only one
        // incremental part exists so far, so its entry is fixed.
        // TODO: read an existing manifest instead of assuming its contents
        // once base files / rewrites are supported.
        if (!File.Exists(ManifestFilePath))
        {
            File.WriteAllText(ManifestFilePath, $"file {IncrFileName} seq 1 type i\n");
        }
    }
}
