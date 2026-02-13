using System.Text.Json.Serialization;

namespace UIHarness.Models;

public sealed class PersonRecord
{
    [JsonPropertyName("personId")]
    public Guid PersonId { get; set; }

    [JsonPropertyName("given")]
    public string Given { get; set; } = string.Empty;

    [JsonPropertyName("family")]
    public string Family { get; set; } = string.Empty;

    [JsonPropertyName("birthdate")]
    public DateOnly Birthdate { get; set; }

    [JsonPropertyName("gender")]
    public string Gender { get; set; } = string.Empty;

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("postcode")]
    public string Postcode { get; set; } = string.Empty;

    [JsonPropertyName("nhsNumber")]
    public string NhsNumber { get; set; } = string.Empty;
}
