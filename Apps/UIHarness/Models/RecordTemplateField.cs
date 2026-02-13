using System.Text.Json.Serialization;

namespace UIHarness.Models;

public sealed class RecordTemplateField
{
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}