using System.IdentityModel.Tokens.Jwt;
using SUI.GetAnIdentifier.API.Models;

namespace SUI.GetAnIdentifier.API.Middleware;

public class AuthContextFactory : IAuthContextFactory
{
    public AuthResult FromJwt(JwtSecurityToken jwt)
    {
        var clientId = Get(jwt, "client_id");

        if (string.IsNullOrWhiteSpace(clientId))
            clientId = Get(jwt, "azp");

        if (string.IsNullOrWhiteSpace(clientId))
            clientId = Get(jwt, "sub");

        if (string.IsNullOrWhiteSpace(clientId))
            return AuthResult.Failure(
                AuthFailureReason.InvalidTokenClaims,
                "Token did not contain client_id, azp, or sub."
            );

        // var scopes = GetScopesFromToken(jwt);

        var context = new AuthContext(clientId);
        return AuthResult.Success(context);

        static string Get(JwtSecurityToken t, string type) =>
            t.Claims.FirstOrDefault(c => c.Type == type)?.Value ?? string.Empty;
    }

    private static List<string> GetScopesFromToken(JwtSecurityToken jwt)
    {
        return jwt
            .Claims.Where(c => c.Type is "scp" or "scope" or "roles" or "role")
            .SelectMany(c =>
                c.Value.Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                )
            )
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
