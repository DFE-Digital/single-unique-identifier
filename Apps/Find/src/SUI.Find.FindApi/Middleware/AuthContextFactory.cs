using System.IdentityModel.Tokens.Jwt;
using SUI.Find.FindApi.Models;
using SUI.Find.Infrastructure.Models;

namespace SUI.Find.FindApi.Middleware;

public class AuthContextFactory : IAuthContextFactory
{
    public AuthContext FromJwt(JwtSecurityToken jwt, AuthStore store)
    {
        var clientId = Get(jwt, "client_id");
        if (string.IsNullOrWhiteSpace(clientId))
            clientId = Get(jwt, "sub");

        if (string.IsNullOrWhiteSpace(clientId))
            throw new InvalidOperationException("Token did not contain client_id or sub.");

        var client = store.Clients?.FirstOrDefault(c => c.ClientId == clientId);
        if (client == null)
            throw new InvalidOperationException("Client could not be found in auth store.");

        if (string.IsNullOrWhiteSpace(client.OrganisationId))
            throw new InvalidOperationException(
                "No Organisation ID found for client in auth store."
            );

        var scopes = jwt
            .Claims.Where(c => c.Type is "scp" or "scope" or "roles" or "role")
            .SelectMany(c =>
                c.Value.Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                )
            )
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new AuthContext(clientId, client.OrganisationId, scopes);

        static string Get(JwtSecurityToken t, string type) =>
            t.Claims.FirstOrDefault(c => c.Type == type)?.Value ?? string.Empty;
    }
}
