using System.Text;

namespace Redis.Common;

// ==============================================================================
// RESP framing
// ==============================================================================

// TCP is a byte stream: one read may hold several commands (pipelining), half
// a command, or a command plus the start of the next one. Framing therefore
// works on bytes and on the lengths RESP declares, never on read boundaries
// or on searching for "\r\n" inside payloads (a bulk string may contain one).
//
// Decoded frames are Latin-1 strings: every byte maps to exactly one char, so
// string lengths equal byte lengths and binary values round-trip unchanged.
public static class RespParser
{
    public class ProtocolException(string message) : Exception(message);

    // Returns the byte length of the first complete frame in `data`, 0 when
    // more bytes are needed, and throws when the bytes can never form a frame.
    public static int MeasureFrame(ReadOnlySpan<byte> data)
    {
        var position = 0;
        return TryMeasureValue(data, ref position) ? position : 0;
    }

    private static bool TryMeasureValue(ReadOnlySpan<byte> data, ref int position)
    {
        if (position >= data.Length)
        {
            return false;
        }

        var type = data[position];
        if (type is not ((byte)'+' or (byte)'-' or (byte)':' or (byte)'$' or (byte)'*'))
        {
            throw new ProtocolException($"unexpected type byte '{(char)type}'");
        }

        if (!TryReadLine(data, ref position, out var line))
        {
            return false;
        }

        switch (type)
        {
            case (byte)'$':
            {
                var length = ParseLength(line);
                if (length < 0)
                {
                    return true; // null bulk string, "$-1"
                }

                // Payload plus its trailing \r\n.
                if (position + length + 2 > data.Length)
                {
                    return false;
                }

                if (data[position + length] != (byte)'\r' || data[position + length + 1] != (byte)'\n')
                {
                    throw new ProtocolException("expected '\\r\\n' after bulk string payload");
                }

                position += length + 2;
                return true;
            }

            case (byte)'*':
            {
                var count = ParseLength(line);
                for (var i = 0; i < count; i++)
                {
                    if (!TryMeasureValue(data, ref position))
                    {
                        return false;
                    }
                }

                return true;
            }

            default:
                return true; // '+', '-' and ':' are a single line
        }
    }

    // Reads up to and including "\r\n". `line` excludes the type byte.
    private static bool TryReadLine(ReadOnlySpan<byte> data, ref int position, out string line)
    {
        var end = data[position..].IndexOf("\r\n"u8);
        if (end < 0)
        {
            line = string.Empty;
            return false;
        }

        line = Encoding.Latin1.GetString(data.Slice(position + 1, end - 1));
        position += end + 2;
        return true;
    }

    private static int ParseLength(string line)
    {
        return int.TryParse(line, out var length) && length >= -1
            ? length
            : throw new ProtocolException($"invalid length '{line}'");
    }

    // Splits a byte buffer that holds whole frames back to back (e.g. an AOF
    // file). A truncated trailing frame, such as one cut off by a crash
    // mid-write, is ignored.
    public static IEnumerable<string> SplitFrames(byte[] data)
    {
        var position = 0;
        while (position < data.Length)
        {
            var length = MeasureFrame(data.AsSpan(position));
            if (length == 0)
            {
                yield break;
            }

            yield return Encoding.Latin1.GetString(data, position, length);
            position += length;
        }
    }
}

// Reads one frame at a time from a connection, keeping leftover bytes of a
// read for the next frame.
public sealed class RespReader(Stream stream)
{
    // Inline commands (plain "PING\r\n", as typed in telnet) have no length
    // prefix, so cap them to keep a client from growing the buffer forever.
    private const int MaxInlineLength = 64 * 1024;

    private byte[] buffer = new byte[4096];
    private int start;
    private int end;

    // Returns the next frame, or null once the peer has closed the connection.
    public string? ReadFrame()
    {
        while (true)
        {
            var available = buffer.AsSpan(start, end - start);

            if (available.Length > 0 && available[0] != (byte)'*' && available[0] != (byte)'$' &&
                available[0] != (byte)'+' && available[0] != (byte)'-' && available[0] != (byte)':')
            {
                var inline = TryTakeInline(available);
                if (inline != null)
                {
                    return inline;
                }
            }
            else if (available.Length > 0)
            {
                var length = RespParser.MeasureFrame(available);
                if (length > 0)
                {
                    var frame = Encoding.Latin1.GetString(available[..length]);
                    start += length;
                    return frame;
                }
            }

            if (!Fill())
            {
                return null;
            }
        }
    }

    // Inline commands are split on whitespace and re-encoded as a RESP array,
    // so the rest of the pipeline only ever sees one shape.
    // TODO: quoted arguments in inline commands are not supported.
    private string? TryTakeInline(ReadOnlySpan<byte> available)
    {
        var newline = available.IndexOf((byte)'\n');
        if (newline < 0)
        {
            if (available.Length > MaxInlineLength)
            {
                throw new RespParser.ProtocolException("too big inline request");
            }

            return null;
        }

        var line = Encoding.Latin1.GetString(available[..newline]).TrimEnd('\r');
        start += newline + 1;

        var arguments = line.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
        return arguments.Length == 0
            ? ReadFrame() // blank line: nothing to run, read on
            : RespBuilder.ArrayFromCommands(arguments);
    }

    // Reads more bytes, compacting or growing the buffer first. Returns false
    // at end of stream.
    private bool Fill()
    {
        if (start > 0)
        {
            Buffer.BlockCopy(buffer, start, buffer, 0, end - start);
            end -= start;
            start = 0;
        }

        if (end == buffer.Length)
        {
            Array.Resize(ref buffer, buffer.Length * 2);
        }

        var read = stream.Read(buffer, end, buffer.Length - end);
        if (read == 0)
        {
            return false;
        }

        end += read;
        return true;
    }
}
