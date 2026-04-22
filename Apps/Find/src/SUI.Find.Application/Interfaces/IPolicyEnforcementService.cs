using SUI.Find.Application.Models;
using SUI.Find.Application.Models.Pep;
using SUI.Find.Application.Services;

namespace SUI.Find.Application.Interfaces;

public interface IPolicyEnforcementService
{
    Task<IReadOnlyList<PepResultItem<TItem>>> FilterItemsAndAuditAsync<TItem>(
        PepFilterInput<TItem> input,
        CancellationToken cancellationToken
    )
        where TItem : IPepFilterable;

    Task CreateAndSendAuditMessageAsync<TItem>(
        IReadOnlyList<PepResultItem<TItem>> resultsWithDecision,
        string destinationOrgId,
        string correlationId,
        string purpose,
        CancellationToken cancellationToken
    )
        where TItem : IPepFilterable;

    Task<PolicyDecisionResult> EvaluateAsync(
        PolicyDecisionRequest request,
        DsaPolicyDefinition dsaPolicy,
        string destOrgType
    );
}
