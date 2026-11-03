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

    public static void Initialize()
    {
        if (!Enabled)
        {
            return;
        }

        // CreateDirectory is a no-op when the directory already exists.
        Directory.CreateDirectory(DirectoryPath);
    }
}
