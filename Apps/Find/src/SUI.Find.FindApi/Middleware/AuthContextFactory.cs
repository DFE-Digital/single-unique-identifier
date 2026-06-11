using System.IdentityModel.Tokens.Jwt;
using SUI.Find.FindApi.Models;
using SUI.Find.Infrastructure.Services;

namespace SUI.Find.FindApi.Middleware;

public class AuthContextFactory : IAuthContextFactory
{
    public AuthContext FromJwt(
        JwtSecurityToken jwt,
        IAuthStoreService storeService,
        bool useAuthStoreForAuthorisation
    )
    {
        var clientId = Get(jwt, "client_id");
        if (string.IsNullOrWhiteSpace(clientId))
            clientId = Get(jwt, "sub");

        if (string.IsNullOrWhiteSpace(clientId))
            throw new InvalidOperationException("Token did not contain client_id or sub.");

        var organisationId = storeService.GetOrganisationIdForClientId(clientId);

        if (string.IsNullOrWhiteSpace(organisationId))
            throw new InvalidOperationException(
                "No Organisation ID found for client in auth store."
            );

        var scopes = useAuthStoreForAuthorisation
            ? storeService.GetScopesByClientId(clientId)
            : GetScopesFromToken(jwt);

        return new AuthContext(clientId, organisationId, scopes);

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
