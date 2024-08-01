using System.Net.Sockets;
using codecrafters_redis.Common;
using codecrafters_redis.Receivers;

namespace codecrafters_redis.Commands;

public class Exec : Base
{
    public override bool CanBePropagated => true;

    protected override async Task OnMasterNodeExecute(Socket socket, CommandDetails commandDetails,
        List<CommandQueueItem> commandQueue, ReceiverBase receiver, bool replicaConnection = false)
    {
        await GenerateCommonResponse(socket, commandDetails, commandQueue, receiver);
    }

    protected override async Task OnReplicaNodeExecute(Socket socket, CommandDetails commandDetails,
        List<CommandQueueItem> commandQueue, ReceiverBase receiver, bool replicaConnection = false)
    {
        await GenerateCommonResponse(socket, commandDetails, commandQueue, receiver);
    }

    private async Task GenerateCommonResponse(Socket socket, CommandDetails commandDetails,
        List<CommandQueueItem> commandQueue, ReceiverBase receiver)
    {
        if (commandQueue.All(x => x.CommandType != CommandType.Multi))
        {
            socket.Send("-ERR EXEC without MULTI\r\n"u8.ToArray());
            return;
        }

        if (commandQueue.Count == 1 && commandQueue.Single().CommandType == CommandType.Multi)
        {
            socket.Send("*0\r\n"u8.ToArray());
            return;
        }

        foreach (var commandInQueue in commandQueue)
        {
            if (commandInQueue.CommandType == CommandType.Multi)
            {
                continue;
            }
            
            // collect command responses here
            await receiver.ExecuteCommand(socket, commandDetails, []);
        }
        
        commandQueue.Clear();
    }
}