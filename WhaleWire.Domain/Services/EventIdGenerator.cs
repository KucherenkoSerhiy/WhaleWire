using System.Security.Cryptography;
using System.Text;

namespace WhaleWire.Domain.Services;

/// <summary>
/// Deterministic EventId builder for testing and production
/// </summary>
public static class EventIdGenerator
{
    public static string Generate(string chain, string address, long lt, string txHash)
    {
        var input = $"{chain}:{address}:{lt}:{txHash}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash).ToLowerInvariant()[..16];
    }

    public static string BuildShort(string chain, long lt, string txHash)
    {
        return Generate(chain, string.Empty, lt, txHash);
    }
}