using System.Text;

namespace codecrafters_redis.Commands;

public class Echo : Base
{
    public override bool CanBePropagated => false;

    protected override async Task<string> OnMasterNodeExecute(CommandContext commandContext)
    {
        return await GenerateCommonResponse(commandContext);
    }

    protected override async Task<string> OnReplicaNodeExecute(CommandContext commandContext)
    {
        return await GenerateCommonResponse(commandContext);
    }

    private static Task<string> GenerateCommonResponse(CommandContext commandContext)
    {
        var response = commandContext.CommandDetails.CommandCount switch
        {
            2 =>
                $"${commandContext.CommandDetails.CommandParts[4].Length}\r\n{commandContext.CommandDetails.CommandParts[4]}\r\n",
            _ => throw new ArgumentException($"Wrong number of arguments for '{nameof(Echo)}' " +
                                             $"command: {commandContext.CommandDetails.CommandCount}.")
        };

        if (!commandContext.ReplicaConnection)
        {
            commandContext.Socket.Send(Encoding.UTF8.GetBytes(response));
        }

        return Task.FromResult(response);
    }
}