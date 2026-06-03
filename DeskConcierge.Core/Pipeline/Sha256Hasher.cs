using System.Security.Cryptography;

namespace DeskConcierge.Core.Pipeline;

public static class Sha256Hasher
{
    public static string Compute(Stream content)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(content);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
