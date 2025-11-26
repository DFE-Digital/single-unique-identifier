using System.Diagnostics.CodeAnalysis;

namespace SUI.Find.FindApi.Models;

[ExcludeFromCodeCoverage(Justification = "Not used yet")]
public record RecordEndpoint
{
    public string ProviderSystem { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public string RecordType { get; set; } = string.Empty;
    public string RecordUrl { get; set; } = string.Empty;
}
