using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using SUI.AuthEmulator.Configurations;

namespace SUI.AuthEmulator.Services;

public class JwtTokenService(
    IOptions<AuthSettings> authSettings,
    IJwksKeyProvider jwksKeyProvider,
    TimeProvider timeProvider
) : IJwtTokenService
{
    private readonly AuthSettings _authSettings = authSettings.Value;

    public string GenerateToken(string clientId, IReadOnlyList<string> scopes)
    {
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
        var selectedKey = signingKeys.First(); //signingKeys.ElementAt(randomIndex);

        // Grab the Asymmetric Signing Credentials directly from the record
        var credentials = selectedKey.SigningCredentials;

        // Calculate token lifetime using the testable TimeProvider
        var now = timeProvider.GetUtcNow();
        var expires = now.AddMinutes(_authSettings.TokenLifetimeMinutes);

        // Build the claims collection using updated configuration settings
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new("client_id", clientId),
            new("scp", string.Join(' ', scopes)),
        };

        // Generate the asymmetric JWT structure
        var jwt = new JwtSecurityToken(
            issuer: _authSettings.Issuer,
            audience: _authSettings.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: credentials
        );

        // Write the token and return
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}
