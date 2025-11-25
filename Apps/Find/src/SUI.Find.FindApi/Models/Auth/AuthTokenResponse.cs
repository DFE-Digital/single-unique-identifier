using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace SUI.Find.FindApi.Models.Auth;

[ExcludeFromCodeCoverage(Justification = "Auth models do not contain any logic to be tested.")]
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
