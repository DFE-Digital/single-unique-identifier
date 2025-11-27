using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace SUI.Find.Infrastructure.Services;

public interface IJwtTokenService
{
    Task<string> GenerateToken(string clientId, IReadOnlyList<string> scopes);
}

public class JwtTokenService(IAuthStoreService authStoreService) : IJwtTokenService
{
    public async Task<string> GenerateToken(string clientId, IReadOnlyList<string> scopes)
    {
        var store = await authStoreService.GetAuthStoreAsync();
        var now = DateTimeOffset.UtcNow;
        var expires = now.AddMinutes(
            store.DefaultTokenLifetimeMinutes > 0 ? store.DefaultTokenLifetimeMinutes : 60
        );

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Iss, store.Issuer),
            new(JwtRegisteredClaimNames.Aud, store.Audience),
            new(
                JwtRegisteredClaimNames.Iat,
                now.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64
            ),
            new(
                JwtRegisteredClaimNames.Nbf,
                now.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64
            ),
            new(
                JwtRegisteredClaimNames.Exp,
                expires.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64
            ),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new("client_id", clientId),
            new("scp", string.Join(' ', scopes)),
        };

        var keyBytes = Encoding.UTF8.GetBytes(store.SigningKey);
        var securityKey = new SymmetricSecurityKey(keyBytes);
        var creds = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: store.Issuer,
            audience: store.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}
