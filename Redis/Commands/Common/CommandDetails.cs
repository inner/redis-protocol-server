using Redis.Common;

namespace Redis.Commands.Common;

public class CommandDetails
{
    public required int CommandCount { get; init; }
    public required string[] CommandParts { get; init; }
    public required string Resp { get; init; }
    public required RespType RespType { get; init; }
    public required bool FromTransaction { get; set; }
}