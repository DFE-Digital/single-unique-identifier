using System.Security.Cryptography;
using Microsoft.Extensions.Options;

namespace SUI.GetAnIdentifier.Infrastructure;

public sealed class AuthTokenServiceConfigValidator : IValidateOptions<AuthTokenServiceConfig>
{
    public ValidateOptionsResult Validate(string? name, AuthTokenServiceConfig options)
    {
        // Leave missing or blank values to the [Required] validator; this validator
        // only checks the format of a supplied private key.
        if (string.IsNullOrWhiteSpace(options.NHS_DIGITAL_PRIVATE_KEY))
        {
            return ValidateOptionsResult.Success;
        }

        try
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(options.NHS_DIGITAL_PRIVATE_KEY);

            // ImportFromPem accepts public keys too. Signing confirms this is
            // a usable RSA private key for the SHA-512 client assertion.
            _ = rsa.SignData(new byte[] { 0 }, HashAlgorithmName.SHA512, RSASignaturePadding.Pkcs1);

            return ValidateOptionsResult.Success;
        }
        catch (ArgumentException)
        {
            return InvalidPrivateKey();
        }
        catch (CryptographicException)
        {
            return InvalidPrivateKey();
        }
    }

    private static ValidateOptionsResult InvalidPrivateKey() =>
        ValidateOptionsResult.Fail(
            "NHS Digital private key must be a valid PEM-encoded RSA private key."
        );
}
