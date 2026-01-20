using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace SUI.StubCustodians.Application.Models;

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
