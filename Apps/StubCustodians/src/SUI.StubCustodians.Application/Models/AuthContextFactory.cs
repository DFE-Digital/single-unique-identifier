using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;

namespace SUI.StubCustodians.Application.Models;

[ExcludeFromCodeCoverage(Justification = "Mock service")]
public static class AuthContextFactory
{
    public static AuthContext FromAuthorizationHeader(string authorizationHeader)
    {
        var token = authorizationHeader["Bearer ".Length..].Trim();
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        static string Get(JwtSecurityToken t, string type) =>
            t.Claims.FirstOrDefault(c => c.Type == type)?.Value ?? string.Empty;

        var subject = Get(jwt, "sub");
        if (string.IsNullOrWhiteSpace(subject))
        {
            subject = Get(jwt, "client_id");
        }

        var organisation = Get(jwt, "org");
        var purpose = Get(jwt, "purpose");

        var roles = jwt
            .Claims.Where(c => c.Type is "roles" or "role" or "scp" or "scope")
            .SelectMany(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var dsas = jwt
            .Claims.Where(c => c.Type is "dsa" or "agreementId")
            .Select(c => c.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new AuthContext(subject, organisation, roles, purpose, dsas);
    }
}
