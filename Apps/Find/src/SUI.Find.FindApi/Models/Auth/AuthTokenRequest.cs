using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace SUI.Find.FindApi.Models.Auth;

[ExcludeFromCodeCoverage(Justification = "Auth models do not contain any logic to be tested.")]
public class AuthTokenRequest
{
    [JsonPropertyName("client_id")]
    public string ClientId { get; init; } = string.Empty;

    [JsonPropertyName("client_secret")]
    public string ClientSecret { get; init; } = string.Empty;

    [JsonPropertyName("scope")]
    public IEnumerable<string> Scopes { get; init; } = [];
}
