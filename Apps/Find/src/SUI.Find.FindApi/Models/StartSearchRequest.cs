using System.Text.Json.Serialization;

namespace SUI.Find.FindApi.Models;

public record StartSearchRequest
{
    [JsonPropertyName("suid")]
    public required string Suid { get; init; }
}
