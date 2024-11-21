using System.Net;
using System.Runtime.InteropServices;
using Redis;
using Redis.Common;
using Redis.Nodes;

var programArgs = args
    .Select(x => x.Replace("\"", string.Empty))
    .ToArray();

var port = Array.IndexOf(programArgs, "--port") != -1
    ? int.Parse(programArgs[Array.IndexOf(programArgs, "--port") + 1])
    : Array.IndexOf(programArgs, "-p") != -1
        ? int.Parse(programArgs[Array.IndexOf(programArgs, "-p") + 1])
        : Constants.DefaultRedisPort;

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
        : Constants.DefaultRedisPort;
    
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
        if (!Directory.Exists(Constants.WindowsMasterDir))
        {
            Directory.CreateDirectory(Constants.WindowsMasterDir);
        }

        if (!Directory.Exists(Constants.WindowsReplicaDir))
        {
            Directory.CreateDirectory(Constants.WindowsReplicaDir);
        }
    }
    else
    {
        if (!Directory.Exists(Constants.LinuxMasterDir))
        {
            Directory.CreateDirectory(Constants.LinuxMasterDir);
        }

        if (!Directory.Exists(Constants.LinuxReplicaDir))
        {
            Directory.CreateDirectory(Constants.LinuxReplicaDir);
        }
    }

    ServerInfo.ServerRuntimeContext.DataDir = ServerInfo.OperatingSystem == OSPlatform.Windows
        ? ServerInfo.ServerRuntimeContext.IsMaster
            ? Constants.WindowsMasterDir
            : Constants.WindowsReplicaDir
        : ServerInfo.ServerRuntimeContext.IsMaster
            ? Constants.LinuxMasterDir
            : Constants.LinuxReplicaDir;

    ServerInfo.ServerRuntimeContext.DbFilename = ServerInfo.ServerRuntimeContext.IsMaster
        ? $"master{(port != 0 ? port : Constants.DefaultRedisPort)}.rdb"
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