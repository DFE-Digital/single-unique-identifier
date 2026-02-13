using System.Text.Json.Serialization;

namespace UIHarness.Models;

public sealed class CustodianContact
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("telephone")]
    public string? Telephone { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [JsonPropertyName("appliesToRecordTypes")]
    public List<string> AppliesToRecordTypes { get; set; } = [];
}