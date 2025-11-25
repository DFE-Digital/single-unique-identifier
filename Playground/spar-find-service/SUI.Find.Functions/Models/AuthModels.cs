using System.IdentityModel.Tokens.Jwt;
using System.Text.Json.Serialization;

namespace Models;

public sealed record AuthContext(
    string ClientId,
    IReadOnlyList<string> Scopes
);

public sealed record PolicyContext(
    string ClientId,
    IReadOnlyList<string> Scopes
);

public sealed class AuthStore
{
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SigningKey { get; set; } = string.Empty;
    public int DefaultTokenLifetimeMinutes { get; set; } = 60;
    public List<AuthClient>? Clients { get; set; }
}

public sealed class AuthClient
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public List<string>? AllowedScopes { get; set; }
}

public sealed class AuthTokenRequest
{
    [JsonPropertyName("client_id")]
    public string ClientId { get; set; } = string.Empty;

    [JsonPropertyName("client_secret")]
    public string ClientSecret { get; set; } = string.Empty;

    [JsonPropertyName("scope")]
    public IEnumerable<string>? Scopes { get; set; }
}

public sealed class AuthTokenFormRequest
{
    public string client_id { get; set; } = string.Empty;
    public string client_secret { get; set; } = string.Empty;
    public string scope { get; set; } = string.Empty;
    public string grant_type { get; set; } = "client_credentials";
}

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

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class RequiredScopesAttribute : Attribute
{
    public RequiredScopesAttribute(params string[] scopes)
    {
        Scopes = scopes ?? Array.Empty<string>();
    }

    public IReadOnlyList<string> Scopes { get; }
}


public sealed class OAuthTokenResponse
{
    public string Access_Token { get; init; } = string.Empty;
    public string Token_Type { get; init; } = "Bearer";
    public int Expires_In { get; init; }
}