using System.Security.Cryptography;
using System.Text;

namespace Resonance.Authentication;

public static class PasswordProof
{
    public static string Create(string password)
    {
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes($"rhythia-password-v1\0{password}")));
    }

    public static string? Normalize(string? proof)
    {
        return proof is { Length: 64 } && proof.All(char.IsAsciiHexDigit) ? proof.ToLowerInvariant() : null;
    }
}
