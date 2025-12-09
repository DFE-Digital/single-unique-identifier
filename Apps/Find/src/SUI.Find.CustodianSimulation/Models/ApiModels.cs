using System.Diagnostics.CodeAnalysis;

namespace SUI.Find.CustodianSimulation.Models;

[ExcludeFromCodeCoverage(Justification = "Mocked simulator")]
public sealed record SearchResultItem(
    string ProviderSystem,
    string ProviderName,
    string RecordType,
    string RecordUrl
);

[ExcludeFromCodeCoverage(Justification = "Mocked simulator")]
public sealed class CustodianRecord
{
    public string RecordId { get; set; } = default!;
    public string PersonId { get; set; } = default!;
    public string RecordType { get; set; } = default!;
    public string DataType { get; set; } = default!;
    public string SchemaUri { get; set; } = default!;
    public System.Text.Json.JsonElement Payload { get; set; }
}

[ExcludeFromCodeCoverage(Justification = "Mocked simulator")]
public sealed record Problem(
    string Type,
    string Title,
    int Status,
    string Detail,
    string? Instance
);

[ExcludeFromCodeCoverage(Justification = "Mocked simulator")]
public sealed record HealthStatus(string Status);

[ExcludeFromCodeCoverage(Justification = "Mocked simulator")]
public abstract record RecordEnvelopeBase(
    string DataType,
    string RecordId,
    string ProviderSystem,
    string Sui
);
