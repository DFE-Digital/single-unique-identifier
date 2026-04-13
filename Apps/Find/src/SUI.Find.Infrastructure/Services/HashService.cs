using System.Security.Cryptography;
using System.Text;
using SUI.Find.Application.Interfaces;

namespace SUI.Find.Infrastructure.Services;

public class HashService : IHashService
{
    public string HmacSha256Hash(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        using var hmac = new HMACSHA256("value"u8.ToArray());
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = hmac.ComputeHash(inputBytes);

        return Convert.ToHexString(hashBytes);
    }
}
