using System.Numerics;
using Redis.Cache;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Bitcount : Base
{
    protected override string Name => nameof(Bitcount);
    public override bool CanBePropagated => false;

    // TODO: the BYTE/BIT range unit argument is not supported; ranges are
    // always byte indexes.
    protected override Task<string> ExecuteCore(CommandContext commandContext)
    {
        var parts = commandContext.CommandDetails.CommandParts;
        string response;

        // Accepted shapes: BITCOUNT key, or BITCOUNT key start end.
        if (parts.Length is not (5 or 9))
        {
            response = RespBuilder.Error("syntax error");
            SendIfNotFromTransaction(commandContext, response);
            return Task.FromResult(response);
        }

        var value = DataCache.Get(parts[4])?.Value ?? string.Empty;
        long start = 0;
        long end = value.Length - 1;

        if (parts.Length == 9)
        {
            if (!long.TryParse(parts[6], out start) || !long.TryParse(parts[8], out end))
            {
                response = RespBuilder.Error("value is not an integer or out of range");
                SendIfNotFromTransaction(commandContext, response);
                return Task.FromResult(response);
            }

            // Negative indexes count from the end, as in Redis; anything that
            // still points before the string is clamped to its first byte.
            if (start < 0) start = Math.Max(0, value.Length + start);
            if (end < 0) end = Math.Max(0, value.Length + end);
        }

        // An end past the string means "up to the last byte"; an empty or
        // inverted range counts nothing.
        end = Math.Min(end, value.Length - 1);

        var count = 0;
        for (var i = start; i <= end; i++)
        {
            // Each char holds one byte of the bitmap (see DataCache.SetBit).
            count += BitOperations.PopCount((uint)(value[(int)i] & 0xFF));
        }

        response = RespBuilder.Integer(count);
        SendIfNotFromTransaction(commandContext, response);
        return Task.FromResult(response);
    }
}
