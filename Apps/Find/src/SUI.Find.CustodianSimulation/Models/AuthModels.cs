using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json.Serialization;

namespace SUI.Find.CustodianSimulation.Models;

[ExcludeFromCodeCoverage(Justification = "Mock service")]
public sealed record AuthContext(
    string Subject,
    string Organisation,
    IReadOnlyList<string> Roles,
    string Purpose,
    IReadOnlyList<string> DsaIds
);

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

[ExcludeFromCodeCoverage(Justification = "Mock service")]
public sealed record PolicyContext(
    string Subject,
    string Organisation,
    IReadOnlyList<string> Roles,
    string Purpose,
    IReadOnlyList<string> DsaIds
);

[ExcludeFromCodeCoverage(Justification = "Mock service")]
public sealed class AuthStore
{
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SigningKey { get; set; } = string.Empty;
    public int DefaultTokenLifetimeMinutes { get; set; } = 60;
    public List<AuthClient>? Clients { get; set; }
}

[ExcludeFromCodeCoverage(Justification = "Mock service")]
public sealed class AuthClient
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public List<string>? AllowedScopes { get; set; }
}

[ExcludeFromCodeCoverage(Justification = "Mock service")]
public sealed class AuthTokenRequest
{
    [JsonPropertyName("client_id")]
    public string ClientId { get; set; } = string.Empty;

    [JsonPropertyName("client_secret")]
    public string ClientSecret { get; set; } = string.Empty;

    [JsonPropertyName("scope")]
    public IEnumerable<string>? Scopes { get; set; }
}

[ExcludeFromCodeCoverage(Justification = "Mock service")]
public sealed class AuthTokenFormRequest
{
    public string client_id { get; set; } = string.Empty;
    public string client_secret { get; set; } = string.Empty;
    public string scope { get; set; } = string.Empty;
    public string grant_type { get; set; } = "client_credentials";
}

[ExcludeFromCodeCoverage(Justification = "Mock service")]
public sealed class AuthTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = "Bearer";

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("scope")]
    public string Scope { get; set; } = string.Empty;
}

[ExcludeFromCodeCoverage(Justification = "Mock service")]
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class RequiredScopesAttribute : Attribute
{
    public RequiredScopesAttribute(params string[]? scopes)
    {
        Scopes = scopes ?? [];
    }

    public IReadOnlyList<string> Scopes { get; }
}
