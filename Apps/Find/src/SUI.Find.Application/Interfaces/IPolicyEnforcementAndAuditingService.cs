using SUI.Find.Application.Models;
using SUI.Find.Application.Models.Pep;
using SUI.Find.Application.Services;

namespace SUI.Find.Application.Interfaces;

public interface IPolicyEnforcementAndAuditingService
{
    Task<IReadOnlyList<PepResultItem<TItem>>> FilterItemsAndAuditAsync<TItem>(
        PepFilterAndAuditInput<TItem> input,
        CancellationToken cancellationToken
    )
        where TItem : IPepFilterable;

    Task CreateAndSendAuditMessageAsync(
        AuditPepFindInput input,
        CancellationToken cancellationToken
    );

    Task<PolicyDecisionResult> EvaluateAsync(
        PolicyDecisionRequest request,
        DsaPolicyDefinition dsaPolicy,
        string destOrgType
    );
}
