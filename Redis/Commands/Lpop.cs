using System.Text;
using Redis.Cache;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Lpop : Base
{
    protected override string Name => nameof(Lpop);
    public override bool CanBePropagated => true;

    protected override Task<string> ExecuteCore(CommandContext commandContext)
    {
        var commands = commandContext.CommandDetails.CommandParts;
        
        var key = commands[4];
        int? count = commands.Length > 6
            ? int.Parse(commands[6])
            : null;

        var result = DataCache.Lpop(key, count ?? null);

        if (result.Length == 0)
        {
            return Task.FromResult(RespBuilder.Null());
        }

        string resp;

        if (count == null)
        {
            resp = RespBuilder.BulkString(result[0]);
        }
        else
        {
            var sb = new StringBuilder(
                RespBuilder.InitArray(result.Length));

            foreach (var item in result)
            {
                sb.Append(RespBuilder.BulkString(item));
            }

            resp = sb.ToString();
        }

        commandContext.Socket.SendCommand(resp);
        return Task.FromResult(resp);
    }
}
