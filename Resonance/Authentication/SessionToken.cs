using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace Resonance.Authentication;

public static class SessionToken
{
    private const string Prefix = "rhythia_v1_";
    private const int EncodedLength = 43;

    public static string Create()
    {
        return Prefix + WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
    }

    public static string? Hash(string? token)
    {
        if (token is null || token.Length != Prefix.Length + EncodedLength ||
            !token.StartsWith(Prefix, StringComparison.Ordinal) ||
            token.AsSpan(Prefix.Length).ContainsAnyExcept("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_"))
            return null;

        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
