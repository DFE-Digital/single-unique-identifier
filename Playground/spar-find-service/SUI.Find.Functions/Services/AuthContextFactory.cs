using System.IdentityModel.Tokens.Jwt;
using Models;

public static class AuthContextFactory
{
    public static AuthContext FromJwt(JwtSecurityToken jwt)
    {
        static string Get(JwtSecurityToken t, string type) =>
            t.Claims.FirstOrDefault(c => c.Type == type)?.Value ?? string.Empty;

        var clientId = Get(jwt, "client_id");
        if (string.IsNullOrWhiteSpace(clientId))
        {
            clientId = Get(jwt, "sub");
        }

        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new InvalidOperationException("Token did not contain client_id or sub.");
        }

        var scopes = jwt.Claims
            .Where(c => c.Type is "scp" or "scope" or "roles" or "role")
            .SelectMany(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new AuthContext(clientId, scopes);
    }
}