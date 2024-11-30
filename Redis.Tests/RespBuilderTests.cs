namespace Redis.Tests;

public class RespBuilderTests
{
    [Fact]
    public void Error_MessagePassed_CorrectRespGenerated()
    {
        const string message = "test message";

        var resp = RespBuilder.Error(message);

        Assert.Equal($"-ERR {message}\r\n", resp);
        Assert.Contains(message, resp);
    }
    
    [Fact]
    public void Error_MessageNull_CorrectRespGenerated()
    {
        const string message = null!;
        Assert.Throws<ArgumentNullException>(() => RespBuilder.Error(message!));
    }

    [Fact]
    public void Null_MessagePassed_CorrectRespGenerated()
    {
        var resp = RespBuilder.Null();
        Assert.Equal("$-1\r\n", resp);
    }
    
    [Fact]
    public void EmptyArray_MessagePassed_CorrectRespGenerated()
    {
        var resp = RespBuilder.EmptyArray();
        Assert.Equal("*0\r\n", resp);
    }
    
    [Fact]
    public void Integer_LongPassed_CorrectRespGenerated()
    {
        const long @long = 123;

        var resp = RespBuilder.Integer(@long);

        Assert.Equal($":{@long}\r\n", resp);
    }
    
    [Fact]
    public void SimpleString_MessagePassed_CorrectRespGenerated()
    {
        const string message = "message";

        var resp = RespBuilder.SimpleString(message);

        Assert.Equal($"+{message}\r\n", resp);
        Assert.Contains(message, resp);
    }
    
    [Fact]
    public void SimpleString_MessageNull_CorrectRespGenerated()
    {
        const string message = null!;
        Assert.Throws<ArgumentNullException>(() => RespBuilder.SimpleString(message!));
    }
    
    [Fact]
    public void BulkString_MessagePassed_CorrectRespGenerated()
    {
        const string message = "message";

        var resp = RespBuilder.BulkString(message);

        Assert.Equal($"${message.Length}\r\n{message}\r\n", resp);
        Assert.Contains(message, resp);
    }
    
    [Fact]
    public void BulkString_MessageNull_CorrectRespGenerated()
    {
        const string message = null!;
        Assert.Throws<ArgumentNullException>(() => RespBuilder.BulkString(message!));
    }
    
    [Fact]
    public void ArrayFromCommands_MessagePassed_CorrectRespGenerated()
    {
        const string command1 = "command1";
        const string command2 = "command12";
        const string command3 = "command123";

        var resp = RespBuilder.ArrayFromCommands(command1, command2, command3);

        Assert.Equal($"*3\r\n" +
                     $"${command1.Length}\r\n{command1}\r\n" +
                     $"${command2.Length}\r\n{command2}\r\n" +
                     $"${command3.Length}\r\n{command3}\r\n", resp);
    }
}