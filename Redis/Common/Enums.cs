namespace Redis.Common;

public enum RespType
{
    Ping,
    Echo,
    Quit,
    Set,
    Get,
    Del,
    Info,
    Replconf,
    Psync,
    Client,
    Wait,
    Type,
    Xadd,
    Xrange,
    Xread,
    Xread2,
    Config,
    Keys,
    Incr,
    Multi,
    Exec,
    Command,
    Discard,
    Exists,
    Rpush,
    Lpush,
    Lrange,
    Llen,
    Lpop,
    Blpop,
    Subscribe,
    Unsubscribe,
    Psubscribe,
    Punsubscribe,
    Publish,
    Zadd,
    Zrank,
    Zrange,
    Zcard,
    Zscore,
    Zrem,
    Geoadd
}

public enum DataType
{
    SimpleString,
    Error,
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