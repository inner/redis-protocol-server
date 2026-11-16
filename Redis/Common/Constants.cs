namespace Redis.Common;

public static class Constants
{
    // regardless of the platform, the newline character is always \r\n
    // as per the RESP protocol
    // Only used to print RESP on a single log line.
    public const string VerbatimNewLine = @"\r\n";
    public const string NewLine = "\r\n";
}