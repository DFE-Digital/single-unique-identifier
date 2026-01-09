using SUI.Find.Application.Models;

namespace SUI.Find.Application.Interfaces;

public interface IPolicyEnforcementService
{
    Task<PolicyDecisionResult> EvaluateAsync(
        PolicyDecisionRequest request,
        DsaPolicyDefinition dsaPolicy,
        string destOrgType,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<SearchResultItem>> FilterResultsAsync(
        string sourceOrgId,
        string destOrgId,
        string destOrgType,
        IReadOnlyList<SearchResultItem> searchResultItems,
        DsaPolicyDefinition dsaPolicy,
        string purpose,
        CancellationToken cancellationToken = default
    );
}
