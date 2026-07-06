using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SUI.AuthEmulator.Configurations;

namespace SUI.AuthEmulator.Services;

public class JwtTokenService(
    IOptions<AuthSettings> authSettings,
    IJwksKeyProvider jwksKeyProvider,
    TimeProvider timeProvider
) : IJwtTokenService
{
    private readonly AuthSettings _authSettings = authSettings.Value;

    public string GenerateToken(string clientId, IReadOnlyList<string> scopes, string? mode = null)
    {
        // Set standard defaults
        var now = timeProvider.GetUtcNow();
        var expires = now.AddMinutes(_authSettings.TokenLifetimeMinutes);
        var issuer = _authSettings.Issuer;
        var audience = _authSettings.Audience;

        // Apply tampering logic based on the mode
        if (!string.IsNullOrWhiteSpace(mode))
        {
            switch (mode.ToLowerInvariant())
            {
                case "not-yet-active":
                    now = timeProvider.GetUtcNow().AddHours(2);
                    expires = timeProvider.GetUtcNow().AddHours(3);
                    break;
                case "expired":
                    now = timeProvider.GetUtcNow().AddHours(-3);
                    expires = timeProvider.GetUtcNow().AddHours(-2);
                    break;
                case "spoof-issuer":
                    issuer = "spoof";
                    break;
                case "spoof-audience":
                    audience = "spoof";
                    break;
            }
        }

        // Build the claims collection using updated configuration settings
        var claims = new List<Claim>
        {
            new(
                JwtRegisteredClaimNames.Iat,
                now.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64
            ),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new("client_id", clientId),
            new("scp", string.Join(' ', scopes)),
        };

        // Determine which signing credentials to use
        SigningCredentials credentials;

        if (mode?.Equals("spoof-private-key", StringComparison.OrdinalIgnoreCase) == true)
        {
            // Generate a fake, on-the-fly RSA key pair
            using var rsa = RSA.Create(2048);
            var fakeKey = new RsaSecurityKey(rsa) { KeyId = Guid.NewGuid().ToString("N") };
            credentials = new SigningCredentials(fakeKey, SecurityAlgorithms.RsaSha256);

            // Generate the asymmetric JWT structure
            var spoofJwt = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: now.UtcDateTime,
                expires: expires.UtcDateTime,
                signingCredentials: credentials
            );

            // Write the token and return while RSA is still alive
            return new JwtSecurityTokenHandler().WriteToken(spoofJwt);
        }

        // Fetch active keys from the JWKS key provider
        var signingKeys = jwksKeyProvider.GetKeys();

        if (signingKeys.Count == 0)
        {
            throw new InvalidOperationException(
                "No RSA signing keys are available from the JWKS provider."
            );
        }

        // Randomly choose an RSA security key securely using CSPRNG
        var randomIndex = RandomNumberGenerator.GetInt32(signingKeys.Count);
        var selectedKey = signingKeys.ElementAt(randomIndex);

        // Grab the Asymmetric Signing Credentials directly from the record
        credentials = selectedKey.SigningCredentials;

        // Generate the asymmetric JWT structure
        var jwt = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: credentials
        );

        // Write the token and return
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}
