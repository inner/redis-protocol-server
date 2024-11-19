namespace Redis.Common;

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
    Command,
    Discard,
    Exists
}

public enum DataType
{
    SimpleString,
    SimpleError,
    Integer,
    BulkString,
    Array
}

public enum EntryIdType
{
    Preset,
    AutoSequence,
    Auto
}