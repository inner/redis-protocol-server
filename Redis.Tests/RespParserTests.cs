using System.Text;
using Redis.Commands.Common;

namespace Redis.Tests;

// Framing and argument parsing, without a server.
public class RespParserTests
{
    private static byte[] Bytes(string value) => Encoding.Latin1.GetBytes(value);

    [Fact]
    public void MeasureFrame_IncompleteArray_ReturnsZero()
    {
        Assert.Equal(0, RespParser.MeasureFrame(Bytes("*2\r\n$3\r\nGET\r\n$3\r\nke")));
    }

    [Fact]
    public void MeasureFrame_PipelinedCommands_ReturnsFirstFrameOnly()
    {
        var first = RespBuilder.ArrayFromCommands("SET", "key", "*2");
        var second = RespBuilder.ArrayFromCommands("GET", "key");

        Assert.Equal(first.Length, RespParser.MeasureFrame(Bytes(first + second)));
    }

    [Fact]
    public void MeasureFrame_UnknownTypeByte_Throws()
    {
        Assert.Throws<RespParser.ProtocolException>(() => RespParser.MeasureFrame(Bytes("?oops\r\n")));
    }

    [Fact]
    public void SplitFrames_IgnoresTruncatedTrailingFrame()
    {
        var whole = RespBuilder.ArrayFromCommands("INCR", "counter");
        var frames = RespParser.SplitFrames(Bytes(whole + whole + "*2\r\n$4\r\nIN")).ToList();

        Assert.Equal([whole, whole], frames);
    }

    [Fact]
    public void BuildCommandDetails_KeepsCrLfAndEmptyValues()
    {
        var details = RespBuilder.ArrayFromCommands("SET", "line\r\nbreak", "").BuildCommandDetails();

        Assert.Equal(3, details.CommandCount);
        Assert.Equal(RespType.Set, details.RespType);
        Assert.Equal("line\r\nbreak", details.CommandParts[4]);
        Assert.Equal(string.Empty, details.CommandParts[6]);
    }

    [Fact]
    public void RespReader_ReadsFramesSplitAcrossReadsAndInlineCommands()
    {
        var input = Bytes(RespBuilder.ArrayFromCommands("ECHO", "hello") + "PING extra\r\n");
        var reader = new RespReader(new OneByteStream(input));

        Assert.Equal(RespBuilder.ArrayFromCommands("ECHO", "hello"), reader.ReadFrame());
        Assert.Equal(RespBuilder.ArrayFromCommands("PING", "extra"), reader.ReadFrame());
        Assert.Null(reader.ReadFrame());
    }

    // Hands out one byte per Read call, the worst case of TCP fragmentation.
    private sealed class OneByteStream(byte[] data) : MemoryStream(data)
    {
        public override int Read(byte[] buffer, int offset, int count) =>
            base.Read(buffer, offset, Math.Min(count, 1));
    }
}
