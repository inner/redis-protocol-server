using System.Net;
using System.Runtime.InteropServices;
using Redis;
using Redis.Nodes;

const int defaultRedisPort = 6379;

var programArgs = args
    .Select(x => x.Replace("\"", string.Empty))
    .ToArray();

var port = Array.IndexOf(programArgs, "--port") != -1
    ? int.Parse(programArgs[Array.IndexOf(programArgs, "--port") + 1])
    : Array.IndexOf(programArgs, "-p") != -1
        ? int.Parse(programArgs[Array.IndexOf(programArgs, "-p") + 1])
        : defaultRedisPort;

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
        : defaultRedisPort;
}

ServerInfo.ServerRuntimeContext.IsMaster = string.IsNullOrEmpty(masterHostString);

if (Array.IndexOf(programArgs, "--dir") != -1)
{
    var dirIndex = Array.IndexOf(programArgs, "--dir");
    var dir = programArgs[dirIndex + 1];
    ServerInfo.ServerRuntimeContext.DataDir = dir;

    var dbFilenameIndex = Array.IndexOf(programArgs, "--dbfilename");
    var dbFilename = programArgs[dbFilenameIndex + 1];
    ServerInfo.ServerRuntimeContext.DbFilename = dbFilename;

    var pathToDbFile = Path.Combine(dir, dbFilename);
    if (File.Exists(pathToDbFile))
    {
        ServerInfo.ServerRuntimeContext.DbFileExists = true;
    }
}
else
{
    const string windowsMasterDir = @"C:\redis-rdb";
    const string windowsReplicaDir = @"C:\redis-rdb\replica";
    const string linuxMasterDir = "/tmp/redis-rdb";
    const string linuxReplicaDir = "/tmp/redis-rdb/replica";

    if (ServerInfo.OperatingSystem == OSPlatform.Windows)
    {
        if (!Directory.Exists(windowsMasterDir))
        {
            Directory.CreateDirectory(windowsMasterDir);
        }

        if (!Directory.Exists(windowsReplicaDir))
        {
            Directory.CreateDirectory(windowsReplicaDir);
        }
    }
    else
    {
        if (!Directory.Exists(linuxMasterDir))
        {
            Directory.CreateDirectory(linuxMasterDir);
        }

        if (!Directory.Exists(linuxReplicaDir))
        {
            Directory.CreateDirectory(linuxReplicaDir);
        }
    }

    ServerInfo.ServerRuntimeContext.DataDir = ServerInfo.OperatingSystem == OSPlatform.Windows
        ? ServerInfo.ServerRuntimeContext.IsMaster
            ? windowsMasterDir
            : windowsReplicaDir
        : ServerInfo.ServerRuntimeContext.IsMaster
            ? linuxMasterDir
            : linuxReplicaDir;

    ServerInfo.ServerRuntimeContext.DbFilename = ServerInfo.ServerRuntimeContext.IsMaster
        ? $"master{(port != 0 ? port : defaultRedisPort)}.rdb"
        : $"replica{port}.rdb";
}

if (Array.IndexOf(programArgs, "--dbfilename") != -1)
{
    var dbFilenameIndex = Array.IndexOf(programArgs, "--dbfilename");
    var dbFilename = programArgs[dbFilenameIndex + 1];
    ServerInfo.ServerRuntimeContext.DbFilename = dbFilename;
}

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