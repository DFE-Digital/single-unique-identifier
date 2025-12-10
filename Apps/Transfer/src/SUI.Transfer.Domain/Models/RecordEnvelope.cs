using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SUI.Transfer.Domain.Models;

public class RecordEnvelope<T>
    where T : class
{
    public required string ProviderSystemId { get; init; }

    public required Uri SchemaUri { get; init; }

    public required T Payload { get; init; }
}
