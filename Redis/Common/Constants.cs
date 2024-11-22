namespace Redis.Common;

public static class Constants
{
    // regardless of the platform, the newline character is always \r\n
    // as per the RESP protocol
    public const string VerbatimNewLine = @"\r\n";
    public const string NewLine = "\r\n";
}