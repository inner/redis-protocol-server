namespace Redis.CommandDocs.Commands;

public static class AclDoc
{
    public static CommandDocumentation Instance { get; } = new(
        Name: "ACL",
        Summary: "Manages the ACL (Access Control List) system.",
        Usage:
        [
            "redis-cli",
            "ACL LIST",
            "ACL SETUSER myuser on >mypassword ~* +@all",
            "ACL GETUSER myuser",
            "ACL DELUSER myuser"
        ]
    );
}
