using System.Net;
using Redis;
using Redis.Nodes;

var programArgs = args
    .Select(x => x.Replace("\"", string.Empty))
    .ToArray();

var port = Array.IndexOf(programArgs, "--port") != -1
    ? int.Parse(programArgs[Array.IndexOf(programArgs, "--port") + 1])
    : Array.IndexOf(programArgs, "-p") != -1
        ? int.Parse(programArgs[Array.IndexOf(programArgs, "-p") + 1])
        : ServerInfo.DefaultRedisPort;

var masterHostString = Array.IndexOf(programArgs, "--replicaof") != -1
    ? programArgs[Array.IndexOf(programArgs, "--replicaof") + 1]
    : null;

string? masterHost = null;
int? masterPort = null;

if (!string.IsNullOrEmpty(masterHostString))
{
    var masterHostParts = masterHostString.Split(' ');
    masterHost = masterHostParts[0];
    masterPort = masterHostParts.Length > 1
        ? int.Parse(masterHostParts[1])
        : ServerInfo.DefaultRedisPort;
    
    ServerInfo.ServerRuntimeContext.IsMaster = false;
}

if (Array.IndexOf(programArgs, "--dir") != -1)
{
    var dirIndex = Array.IndexOf(programArgs, "--dir");
    var dir = programArgs[dirIndex + 1];
    ServerInfo.ServerRuntimeContext.DataDir = dir;

    // --dbfilename is optional alongside --dir (e.g. AOF-only setups).
    var dbFilenameIndex = Array.IndexOf(programArgs, "--dbfilename");
    ServerInfo.ServerRuntimeContext.DbFilename = dbFilenameIndex != -1
        ? programArgs[dbFilenameIndex + 1]
        : "dump.rdb";

    var pathToDbFile = Path.Combine(dir, ServerInfo.ServerRuntimeContext.DbFilename);
    if (File.Exists(pathToDbFile))
    {
        ServerInfo.ServerRuntimeContext.DbFileExists = true;
    }
}
else
{
    // Like Redis, `dir` defaults to the working directory at startup.
    ServerInfo.ServerRuntimeContext.DataDir = Directory.GetCurrentDirectory();

    ServerInfo.ServerRuntimeContext.DbFilename = ServerInfo.ServerRuntimeContext.IsMaster
        ? $"master{(port != 0 ? port : ServerInfo.DefaultRedisPort)}{ServerInfo.RdbExtension}"
        : $"replica{port}{ServerInfo.RdbExtension}";
}

if (Array.IndexOf(programArgs, "--dbfilename") != -1)
{
    var dbFilenameIndex = Array.IndexOf(programArgs, "--dbfilename");
    var dbFilename = programArgs[dbFilenameIndex + 1];
    ServerInfo.ServerRuntimeContext.DbFilename = dbFilename;
}

// AOF options only need to be recorded here; defaults live in
// ServerRuntimeContext and are reported by CONFIG GET.
string? ReadOption(string name)
{
    var index = Array.IndexOf(programArgs, $"--{name}");
    return index != -1 && index + 1 < programArgs.Length ? programArgs[index + 1] : null;
}

var aof = ServerInfo.ServerRuntimeContext.Aof;
aof.AppendOnly = ReadOption("appendonly") ?? aof.AppendOnly;
aof.AppendDirName = ReadOption("appenddirname") ?? aof.AppendDirName;
aof.AppendFilename = ReadOption("appendfilename") ?? aof.AppendFilename;
aof.AppendFsync = ReadOption("appendfsync") ?? aof.AppendFsync;

try
{
    if (ServerInfo.ServerRuntimeContext.IsMaster)
    {
        new MasterNode(IPAddress.Any, port)
            .Start();
    }
    else
    {
        new ReplicaNode(IPAddress.Any, port, masterHost!, masterPort!.Value)
            .Handshake()
            .Start();
    }
}
catch (Exception ex)
{
    Console.WriteLine($"{ex.Message}, stack: {ex.StackTrace}");
    throw;
}