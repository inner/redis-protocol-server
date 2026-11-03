using Redis.Cache;
using Redis.Commands.Common;
using Redis.Common;

namespace Redis.Commands;

public class Bitop : Base
{
    protected override string Name => nameof(Bitop);
    public override bool CanBePropagated => true;

    protected override Task<string> ExecuteCore(CommandContext commandContext)
    {
        var parts = commandContext.CommandDetails.CommandParts;
        string response;

        // BITOP operation destkey key [key ...]
        if (parts.Length < 9)
        {
            response = RespBuilder.Error("wrong number of arguments for 'bitop' command");
            SendIfNotFromTransaction(commandContext, response);
            return Task.FromResult(response);
        }

        var operation = parts[4].ToUpperInvariant();
        var destination = parts[6];
        var sources = new List<string>();
        for (var i = 8; i < parts.Length; i += 2)
        {
            // Each char of a stored value holds one byte (see DataCache.SetBit);
            // missing keys behave as empty strings.
            sources.Add(DataCache.Get(parts[i])?.Value ?? string.Empty);
        }

        if (operation is not ("AND" or "OR" or "XOR" or "NOT") ||
            (operation == "NOT" && sources.Count != 1))
        {
            response = RespBuilder.Error(operation == "NOT"
                ? "BITOP NOT must be called with a single source key."
                : "syntax error");
            SendIfNotFromTransaction(commandContext, response);
            return Task.FromResult(response);
        }

        // The result is as long as the longest source; shorter sources are
        // treated as if padded with zero bytes, as Redis does.
        var length = sources.Max(s => s.Length);
        var result = new char[length];

        for (var byteIndex = 0; byteIndex < length; byteIndex++)
        {
            int ByteAt(string source) => byteIndex < source.Length ? source[byteIndex] & 0xFF : 0;

            var combined = ByteAt(sources[0]);
            foreach (var source in sources.Skip(1))
            {
                combined = operation switch
                {
                    "AND" => combined & ByteAt(source),
                    "OR" => combined | ByteAt(source),
                    _ => combined ^ ByteAt(source)
                };
            }

            if (operation == "NOT")
            {
                combined = ~combined & 0xFF;
            }

            result[byteIndex] = (char)combined;
        }

        // An empty result removes the destination instead of storing "".
        if (length == 0)
        {
            DataCache.DelKeys(destination);
        }
        else
        {
            DataCache.Set(destination, new string(result));
        }

        response = RespBuilder.Integer(length);
        SendIfNotFromTransaction(commandContext, response);
        return Task.FromResult(response);
    }
}
