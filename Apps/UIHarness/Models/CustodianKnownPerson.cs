using System.Text.Json.Serialization;

namespace UIHarness.Models;

public sealed class CustodianKnownPerson
{
    [JsonPropertyName("nhsNumber")]
    public string NhsNumber { get; set; } = string.Empty;

    [JsonPropertyName("recordTypes")]
    public List<string> RecordTypes { get; set; } = [];
}
