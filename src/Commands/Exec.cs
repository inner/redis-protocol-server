using System.Net.Sockets;
using codecrafters_redis.Enums;
using codecrafters_redis.Receivers;

namespace codecrafters_redis.Commands;

public class Exec : Base
{
    public override bool CanBePropagated => true;

    protected override async Task OnMasterNodeExecute(Socket socket, int commandCount, string[] commandParts,
        List<CommandQueueItem> commandQueue, ReceiverBase receiver, bool replicaConnection = false)
    {
        await GenerateCommonResponse(socket, commandParts, commandQueue, receiver, replicaConnection);
    }

    protected override async Task OnReplicaNodeExecute(Socket socket, int commandCount, string[] commandParts,
        List<CommandQueueItem> commandQueue, ReceiverBase receiver, bool replicaConnection = false)
    {
        await GenerateCommonResponse(socket, commandParts, commandQueue, receiver, replicaConnection);
    }

    private async Task GenerateCommonResponse(Socket socket, string[] commandParts,
        List<CommandQueueItem> commandQueue, ReceiverBase receiver, bool replicaConnection = false)
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
            
            await receiver.Receive(socket, commandInQueue.CommandString, [], countBytes: false);
        }
        
        commandQueue.Clear();
    }
}