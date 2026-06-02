using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using SUI.AuthEmulator.Models;

namespace SUI.AuthEmulator.Services;

public class JwksKeyProvider : IJwksKeyProvider
{
    // Stored in a private field
    private readonly RsaKeyDetails[] _rsaKeys;

    public JwksKeyProvider()
    {
        // Generates 4 RSA Keys in the constructor
        _rsaKeys = Enumerable
            .Range(0, 4)
            .Select(id =>
            {
                // Create 2048-bit RSA key pair
                var rsa = RSA.Create(2048);

                // Export only public parameters (false) for JWKS encoding safely
                var publicParams = rsa.ExportParameters(false);

                var rsaSecurityKey = new RsaSecurityKey(rsa)
                {
                    KeyId = $"{DateTime.UtcNow:o}_{id}",
                };

                return new RsaKeyDetails(
                    rsaSecurityKey,
                    new SigningCredentials(rsaSecurityKey, SecurityAlgorithms.RsaSha256),
                    Base64UrlEncoder.Encode(publicParams.Modulus),
                    Base64UrlEncoder.Encode(publicParams.Exponent)
                );
            })
            .ToArray();
    }

    public IReadOnlyCollection<RsaKeyDetails> GetKeys() => _rsaKeys;
}
