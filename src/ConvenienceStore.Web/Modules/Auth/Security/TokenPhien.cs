using System.Security.Cryptography;
using System.Text;

namespace ConvenienceStore.Web.Modules.Auth.Security;

public static class TokenPhien
{
    public static string TaoToken() => Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));

    public static string Bam(string token) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public static string TaoXsrf(string token) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes("xsrf:" + token)));

    public static bool KhopXsrf(string token, string? giaTriGuiLen) =>
        !string.IsNullOrEmpty(giaTriGuiLen)
        && CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(TaoXsrf(token)), Encoding.ASCII.GetBytes(giaTriGuiLen));
}
