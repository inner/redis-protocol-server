using System.Security.Cryptography;

namespace Redis.Common;

public static class PasswordHasher
{
    public static string EncryptPassword(string password)
    {
        var passwordBytes = password.AsBytes();
        var hashBytes = SHA256.HashData(passwordBytes);
        var passwordHash = BitConverter
            .ToString(hashBytes)
            .Replace("-", string.Empty)
            .ToLowerInvariant();
        
        return passwordHash;
    }
    
    public static bool VerifyPassword(string password, string? storedHash)
    {
        var computedHash = EncryptPassword(password);
        return string.Equals(computedHash, storedHash, StringComparison.InvariantCultureIgnoreCase);
    }
}