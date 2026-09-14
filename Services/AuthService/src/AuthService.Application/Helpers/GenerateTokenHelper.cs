using System.Security.Cryptography;
using System.Text;

namespace AuthService.Application.Helpers;

public static class GenerateTokenHelper
{
    public static string ComputeSha256(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));

        return Convert.ToHexString(bytes);
    }

    public static string ComputeHmacSha256(string secret, string value)
    {
        var key = Encoding.UTF8.GetBytes(secret);
        var bytes = HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(value));

        return Convert.ToHexString(bytes);
    }

    public static string GenerateResetToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);

        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }

    public static string GenerateNumericOtp(int length = 6)
    {
        var max = (int)Math.Pow(10, length);
        return RandomNumberGenerator.GetInt32(0, max).ToString($"D{length}");
    }
}
