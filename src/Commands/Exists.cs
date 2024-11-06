using System.Text;

namespace codecrafters_redis.Commands;

public class Exists : Base
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
        if (commandContext.CommandDetails.CommandParts.Length < 2)
        {
            throw new ArgumentException(
                $"Wrong number of arguments for '{nameof(Exists)}' command: {commandContext.CommandDetails.CommandParts.Length}.");
        }

        var exists = 1;
        var response = $":{exists}\r\n";

        commandContext.Socket.Send(Encoding.UTF8.GetBytes(response));
        return Task.FromResult(response);
    }
}