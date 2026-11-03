using System.Text;
using Redis.Cache;
using Redis.Commands.Common;
using Redis.Common;
using Redis.Receivers;

namespace Redis.Commands;

public class Exec : Base
{
    protected override string Name => nameof(Exec);
    public override bool CanBePropagated => true;

    protected override async Task<string> ExecuteCore(CommandContext commandContext)
    {
        string result;

        if (commandContext.CommandQueue.All(x => x.RespType != RespType.Multi))
        {
            result = RespBuilder.Error("EXEC without MULTI");
            commandContext.Socket.SendCommand(result);
            return result;
        }

        // EXEC ends the connection's optimistic-locking session whatever the
        // outcome, so read the dirty flag first and clear the watches here.
        var watchedKeyTouched = WatchRegistry.IsDirty(commandContext.Socket);
        WatchRegistry.Unwatch(commandContext.Socket);

        if (watchedKeyTouched)
        {
            // Aborted transaction: queued commands are dropped unexecuted and
            // the client gets a null array, as in Redis.
            result = RespBuilder.NullArray();
            commandContext.Socket.SendCommand(result);
            commandContext.CommandQueue.Clear();
            return result;
        }

        if (commandContext.CommandQueue.Count == 1 &&
            commandContext.CommandQueue.Single().RespType == RespType.Multi)
        {
            result = RespBuilder.EmptyArray();
            commandContext.Socket.SendCommand(result);
            commandContext.CommandQueue.Clear();
            return result;
        }

        List<string> commandResults = [];

        foreach (var commandInQueue in commandContext.CommandQueue)
        {
            if (commandInQueue.RespType == RespType.Multi)
            {
                continue;
            }

            var commandDetails = commandInQueue.Resp.BuildCommandDetails();
            commandDetails.FromTransaction = true;
            
            commandResults.Add(
                await commandContext.Receiver
                    .ExecuteCommand(
                        commandContext.Socket,
                        commandDetails,
                        [],
                        [],
                        commandContext.Source));
        }

        var sb = new StringBuilder($"*{commandResults.Count}\r\n");
        foreach (var commandResult in commandResults)
        {
            sb.Append($"{commandResult}");
        }
        
        commandContext.Socket.SendCommand(sb.ToString());
        commandContext.CommandQueue.Clear();
        
        return string.Empty;
    }
}
