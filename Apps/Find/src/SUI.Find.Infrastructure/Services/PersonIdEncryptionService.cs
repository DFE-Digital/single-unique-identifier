using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using SUI.Find.Application.Interfaces;
using SUI.Find.Domain.Models;

namespace SUI.Find.Infrastructure.Services;

public class PersonIdEncryptionService(ILogger<PersonIdEncryptionService> logger)
    : IPersonIdEncryptionService
{
    private const string SupportedAlgorithm = "AES-256-CBC";
    private const string NhsPrefix = "NHS:";
    private const int NhsNumberLength = 10;
    private const int AesBlockSize = 16;
    private const int Aes256KeySize = 32;

    public Result<string> EncryptNhsToPersonId(string nhsNumber, EncryptionDefinition encryption)
    {
        try
        {
            EnsureSupported(encryption);

            var keyBytes = Convert.FromBase64String(encryption.Key);
            EnsureKeyLength(keyBytes, encryption.KeyId);

            var digits = new string(nhsNumber.Where(char.IsDigit).ToArray());
            if (digits.Length > NhsNumberLength)
            {
                digits = digits[^NhsNumberLength..];
            }

            digits = digits.PadLeft(NhsNumberLength, '0');

            var plaintext = $"{NhsPrefix}{digits}";
            var pt = Encoding.ASCII.GetBytes(plaintext);

            var block = new byte[AesBlockSize];
            Buffer.BlockCopy(pt, 0, block, 0, Math.Min(pt.Length, block.Length));

            // Use zero IV for deterministic encryption (acceptable for single-block, format-preserving encryption)
            // Consider having a deterministic IV in future.
            var iv = new byte[AesBlockSize];
            using var aes = CreateAes(keyBytes, iv);

            using var encryptor = aes.CreateEncryptor();
            var ct = encryptor.TransformFinalBlock(block, 0, block.Length);

            return Result<string>.Ok(Base64UrlEncodeNoPadding(ct));
        }
        catch (FormatException ex)
        {
            logger.LogError(ex, "Invalid base64 key format for KeyId '{KeyId}'", encryption.KeyId);
            return Result<string>.Fail("Invalid encryption key format");
        }
        catch (CryptographicException ex)
        {
            logger.LogError(ex, "Encryption failed for KeyId '{KeyId}'", encryption.KeyId);
            return Result<string>.Fail($"Encryption failed: {ex.Message}");
        }
        catch (NotSupportedException ex)
        {
            logger.LogError(ex, "Unsupported algorithm '{Algorithm}'", encryption.Algorithm);
            return Result<string>.Fail(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex, "Invalid operation during encryption");
            return Result<string>.Fail(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during encryption");
            return Result<string>.Fail($"Unexpected encryption error: {ex.Message}");
        }
    }

    public Result<string> DecryptPersonIdToNhs(string personId, EncryptionDefinition encryption)
    {
        try
        {
            EnsureSupported(encryption);

            var keyBytes = Convert.FromBase64String(encryption.Key);
            EnsureKeyLength(keyBytes, encryption.KeyId);

            var ct = Base64UrlDecodeNoPadding(personId);

            // Use same zero IV as encryption
            var iv = new byte[AesBlockSize];
            using var aes = CreateAes(keyBytes, iv);

            using var decryptor = aes.CreateDecryptor();
            var pt = decryptor.TransformFinalBlock(ct, 0, ct.Length);

            var text = Encoding.ASCII.GetString(pt).TrimEnd('\0');

            if (!text.StartsWith(NhsPrefix, StringComparison.OrdinalIgnoreCase))
            {
                logger.LogError(
                    "Decryption failed to produce expected NHS format with KeyId '{KeyId}'.",
                    encryption.KeyId
                );
                return Result<string>.Fail(
                    $"Ciphertext did not decrypt to expected '{NhsPrefix}' format"
                );
            }

            return Result<string>.Ok(text[NhsPrefix.Length..]);
        }
        catch (FormatException ex)
        {
            logger.LogError(
                ex,
                "Invalid base64 format during decryption for KeyId '{KeyId}'",
                encryption.KeyId
            );
            return Result<string>.Fail("Invalid PersonId or key format");
        }
        catch (CryptographicException ex)
        {
            logger.LogError(ex, "Decryption failed for KeyId '{KeyId}'", encryption.KeyId);
            return Result<string>.Fail($"Decryption failed: {ex.Message}");
        }
        catch (NotSupportedException ex)
        {
            logger.LogError(ex, "Unsupported algorithm '{Algorithm}'", encryption.Algorithm);
            return Result<string>.Fail(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex, "Invalid operation during decryption");
            return Result<string>.Fail(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during decryption");
            return Result<string>.Fail($"Unexpected decryption error: {ex.Message}");
        }
    }

    private void EnsureKeyLength(byte[] keyBytes, string keyId)
    {
        if (keyBytes.Length != Aes256KeySize)
        {
            logger.LogError("Invalid encryption key length for KeyId '{KeyId}':", keyId);
            throw new InvalidOperationException(
                $"Encryption key '{keyId}' must be {Aes256KeySize} bytes (base64 for AES-256)."
            );
        }
    }

    [ExcludeFromCodeCoverage(
        Justification = "Factory method with exception-safety pattern - catch block extremely difficult to trigger"
    )]
    private static Aes CreateAes(byte[] keyBytes, byte[] iv)
    {
        Aes? aes = null;
        try
        {
            // https://learn.microsoft.com/en-us/dotnet/api/System.Security.Cryptography.CipherMode?view=net-9.0
            aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.None;
            aes.Key = keyBytes;
            aes.IV = iv;
            return aes;
        }
        catch
        {
            aes?.Dispose();
            throw;
        }
    }

    private static void EnsureSupported(EncryptionDefinition encryption)
    {
        if (
            !string.Equals(
                encryption.Algorithm,
                SupportedAlgorithm,
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            throw new NotSupportedException($"Unsupported algorithm '{encryption.Algorithm}'.");
        }
    }

    private static string Base64UrlEncodeNoPadding(byte[] bytes)
    {
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static byte[] Base64UrlDecodeNoPadding(string s)
    {
        var b64 = s.Replace('-', '+').Replace('_', '/');
        switch (b64.Length % 4)
        {
            case 2:
                b64 += "==";
                break;
            case 3:
                b64 += "=";
                break;
        }
        return Convert.FromBase64String(b64);
    }
}
