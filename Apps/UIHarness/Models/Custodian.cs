using System.Text.Json.Serialization;

namespace UIHarness.Models;

public sealed class Custodian
{
    [JsonPropertyName("custodianId")]
    public string CustodianId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("recordTypes")]
    public List<string> RecordTypes { get; set; } = [];

    [JsonPropertyName("contacts")]
    public List<CustodianContact> Contacts { get; set; } = [];

    [JsonPropertyName("knownPeople")]
    public List<CustodianKnownPerson> KnownPeople { get; set; } = [];
}

