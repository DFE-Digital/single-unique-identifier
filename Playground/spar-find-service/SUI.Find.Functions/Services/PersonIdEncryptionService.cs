using Interfaces;
using Models;
using System.Security.Cryptography;
using System.Text;

namespace Services;

public sealed class PersonIdEncryptionService : IPersonIdEncryptionService
{
    public string EncryptNhsToPersonId(string nhsNumber, EncryptionDefinition encryption)
    {
        EnsureSupported(encryption);

        var keyBytes = Convert.FromBase64String(encryption.Key);
        if (keyBytes.Length != 32)
        {
            throw new InvalidOperationException($"Encryption key '{encryption.KeyId}' must be 32 bytes (base64 for AES-256).");
        }

        var digits = new string(nhsNumber.Where(char.IsDigit).ToArray());
        if (digits.Length > 10)
        {
            digits = digits[^10..];
        }

        digits = digits.PadLeft(10, '0');

        var plaintext = $"NHS:{digits}";
        var pt = Encoding.ASCII.GetBytes(plaintext);

        var block = new byte[16];
        Buffer.BlockCopy(pt, 0, block, 0, Math.Min(pt.Length, block.Length));

        using var aes = Aes.Create();
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        aes.Key = keyBytes;

        using var encryptor = aes.CreateEncryptor();
        var ct = encryptor.TransformFinalBlock(block, 0, block.Length);

        return Base64UrlEncodeNoPadding(ct);
    }

    public string DecryptPersonIdToNhs(string personId, EncryptionDefinition encryption)
    {
        EnsureSupported(encryption);

        var keyBytes = Convert.FromBase64String(encryption.Key);

        var ct = Base64UrlDecodeNoPadding(personId);

        using var aes = Aes.Create();
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        aes.Key = keyBytes;

        using var decryptor = aes.CreateDecryptor();
        var pt = decryptor.TransformFinalBlock(ct, 0, ct.Length);

        var text = Encoding.ASCII.GetString(pt).TrimEnd('\0');

        if (!text.StartsWith("NHS:", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Ciphertext did not decrypt to expected 'NHS:' format.");
        }

        return text["NHS:".Length..];
    }

    private static void EnsureSupported(EncryptionDefinition encryption)
    {
        if (!string.Equals(encryption.Algorithm, "AES-256-ECB", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException($"Unsupported algorithm '{encryption.Algorithm}'.");
        }
    }

    private static string Base64UrlEncodeNoPadding(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static byte[] Base64UrlDecodeNoPadding(string s)
    {
        var b64 = s.Replace('-', '+').Replace('_', '/');
        switch (b64.Length % 4)
        {
            case 2: b64 += "=="; break;
            case 3: b64 += "="; break;
        }
        return Convert.FromBase64String(b64);
    }
}
