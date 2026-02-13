using System.Text.Json.Serialization;

namespace UIHarness.Models;

public sealed class RecordTemplateSection
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("fields")]
    public List<RecordTemplateField> Fields { get; set; } = [];
}
