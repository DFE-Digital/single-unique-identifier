using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SUI.Transfer.Domain.Models;

public class RecordEnvelope<T>
    where T : class
{
    [JsonPropertyName("schemaUri")]
    public required Uri SchemaUri { get; init; }

    [JsonPropertyName("payload")]
    public required T Payload { get; init; }
}
