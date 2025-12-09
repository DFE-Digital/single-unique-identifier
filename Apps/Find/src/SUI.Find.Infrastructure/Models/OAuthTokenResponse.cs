using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace SUI.Find.Infrastructure.Models;

public sealed record OAuthTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; init; } = string.Empty;

    [JsonPropertyName("token_type")]
    public string TokenType { get; init; } = "Bearer";

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; init; }
}
