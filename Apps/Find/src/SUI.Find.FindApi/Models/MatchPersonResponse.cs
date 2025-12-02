using System.Text.Json.Serialization;

namespace SUI.Find.FindApi.Models;

public class MatchPersonResponse
{
    [JsonPropertyName("suid")]
    public required string Suid { get; set; }
}
