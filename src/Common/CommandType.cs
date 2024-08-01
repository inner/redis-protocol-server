namespace codecrafters_redis.Common;

public enum CommandType
{
    Ping,
    Echo,
    Quit,
    Set,
    Get,
    Info,
    Replconf,
    Psync,
    Client,
    Wait,
    Type,
    Xadd,
    Xrange,
    Xread,
    Config,
    Keys,
    Incr,
    Multi,
    Exec,
    Command
}