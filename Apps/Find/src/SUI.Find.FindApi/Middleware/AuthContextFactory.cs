using System.IdentityModel.Tokens.Jwt;
using SUI.Find.FindApi.Models;
using SUI.Find.FindApi.Models.Auth;
using SUI.Find.Infrastructure.Services;

namespace SUI.Find.FindApi.Middleware;

public class AuthContextFactory(IAuthStoreService storeService) : IAuthContextFactory
{
    public AuthResult FromJwt(JwtSecurityToken jwt, bool useAuthStoreForAuthorisation)
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

        var client = storeService.GetClientById(clientId);

        if (client == null)
            return AuthResult.Failure(
                AuthFailureReason.ClientNotFound,
                "No matching client found in auth store."
            );

        if (!client.Enabled)
            return AuthResult.Failure(AuthFailureReason.ClientDisabled, "Client is disabled.");

        var organisationId = client.OrganisationId;

        if (string.IsNullOrWhiteSpace(organisationId))
            return AuthResult.Failure(
                AuthFailureReason.MissingOrganisationId,
                "No Organisation ID found for client in auth store."
            );

        var scopes = useAuthStoreForAuthorisation
            ? client.AllowedScopes ?? []
            : GetScopesFromToken(jwt);

        var context = new AuthContext(clientId, organisationId, scopes);
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
