using System.Security.Cryptography;
using System.Text;

namespace SUI.Find.FindApi.Utility;

public static class HashUtility
{
    public static string HashInput(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes("value"));
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = hmac.ComputeHash(inputBytes);

        return Convert.ToHexString(hashBytes);
    }
}