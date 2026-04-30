using SUI.Find.Application.Models;

namespace SUI.Find.Application.Services;

/// <summary>
/// Input object for the PolicyEnforcementAndAuditingService.
/// That service determines which of the specified items can be shared from the Source Organization (Custodian of data being shared) to the Destination Organization (Searcher/Requestor).
/// </summary>
/// <param name="SourceOrgId">Organization ID of the Custodian that owns the data being shared.</param>
/// <param name="DestOrgId">Organization ID performing the search or making the request for the data, i.e. who the data is being shared to.</param>
/// <param name="DestOrgType">Organization Type of the Searcher/Requestor.</param>
/// <param name="Items">The items that represent the data being shared from the Source Organization to the Destination Organization.</param>
/// <param name="DsaPolicy">The Data Sharing Agreement policy belonging to the Source Organization.</param>
/// <param name="Purpose">The reason why the Searcher/Requestor requested the data, and why they need to see the data.</param>
/// <param name="CorrelationId">ID used to correlate this action to the other steps in this search process.</param>
public record PepFilterInput<TItem>(
    string SourceOrgId,
    string DestOrgId,
    string DestOrgType,
    IReadOnlyList<TItem> Items,
    DsaPolicyDefinition DsaPolicy,
    string Purpose,
    string CorrelationId,
    string? TraceParent
)
    where TItem : IPepFilterable;
