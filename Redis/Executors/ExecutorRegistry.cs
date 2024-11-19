using Redis.Common;

namespace Redis.Executors;

public static class ExecutorRegistry
{
    public static readonly Dictionary<DataType, IRespDataTypeExecutor> Executors = new()
    {
        { DataType.Array, new ArrayExecutor() },
        { DataType.SimpleString, new SimpleStringExecutor() },
        { DataType.Error, new ErrorExecutor() },
        { DataType.Integer, new IntegerExecutor() },
        { DataType.BulkString, new BulkStringExecutor() }
    };
}