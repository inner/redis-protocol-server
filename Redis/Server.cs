using System.Net;
using System.Runtime.InteropServices;
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
    if (ServerInfo.OperatingSystem == OSPlatform.Windows)
    {
        if (!Directory.Exists(ServerInfo.WindowsMasterDir))
        {
            Directory.CreateDirectory(ServerInfo.WindowsMasterDir);
        }

        if (!Directory.Exists(ServerInfo.WindowsReplicaDir))
        {
            Directory.CreateDirectory(ServerInfo.WindowsReplicaDir);
        }
    }
    else
    {
        if (!Directory.Exists(ServerInfo.LinuxMasterDir))
        {
            Directory.CreateDirectory(ServerInfo.LinuxMasterDir);
        }

        if (!Directory.Exists(ServerInfo.LinuxReplicaDir))
        {
            Directory.CreateDirectory(ServerInfo.LinuxReplicaDir);
        }
    }

    ServerInfo.ServerRuntimeContext.DataDir = ServerInfo.OperatingSystem == OSPlatform.Windows
        ? ServerInfo.ServerRuntimeContext.IsMaster
            ? ServerInfo.WindowsMasterDir
            : ServerInfo.WindowsReplicaDir
        : ServerInfo.ServerRuntimeContext.IsMaster
            ? ServerInfo.LinuxMasterDir
            : ServerInfo.LinuxReplicaDir;

    ServerInfo.ServerRuntimeContext.DbFilename = ServerInfo.ServerRuntimeContext.IsMaster
        ? $"master{(port != 0 ? port : ServerInfo.DefaultRedisPort)}.rdb"
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