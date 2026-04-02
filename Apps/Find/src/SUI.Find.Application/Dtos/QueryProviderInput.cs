using SUI.Find.Application.Models;

namespace SUI.Find.Application.Dtos;

public sealed record QueryProviderInput(
    string RequestingOrg,
    string JobId,
    string InvocationId,
    string Suid,
    ProviderDefinition Provider
)
{
    /// <summary>
    /// The Organisation requesting the data, i.e. the Searcher.
    /// </summary>
    public string RequestingOrg { get; } = RequestingOrg;

    /// <summary>
    /// The Provider being queried, i.e. the Custodian of the data.
    /// </summary>
    public ProviderDefinition Provider { get; } = Provider;

    public string? WorkItemId { get; init; }
}
