using System.Text.Json.Serialization;

namespace UIHarness.Models;

public sealed class RecordTemplate
{
    [JsonPropertyName("recordType")]
    public string RecordType { get; set; } = string.Empty;

    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    [JsonPropertyName("sections")]
    public List<RecordTemplateSection> Sections { get; set; } = [];
}
