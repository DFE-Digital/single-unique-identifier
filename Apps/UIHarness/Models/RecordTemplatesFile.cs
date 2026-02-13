using System.Text.Json.Serialization;

namespace UIHarness.Models;

public sealed class RecordTemplatesFile
{
    [JsonPropertyName("templates")]
    public List<RecordTemplate> Templates { get; set; } = [];
}
