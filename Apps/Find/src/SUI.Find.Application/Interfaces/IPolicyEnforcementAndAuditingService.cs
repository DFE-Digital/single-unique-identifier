using SUI.Find.Application.Models;
using SUI.Find.Application.Models.Pep;

namespace SUI.Find.Application.Interfaces;

public interface IPolicyEnforcementAndAuditingService
{
    Task<IReadOnlyList<PepResultItem<CustodianSearchResultItem>>> FilterItemsAndAuditAsync(
        PepContext context,
        List<CustodianSearchResultItem> input,
        string invocationId,
        string purpose,
        CancellationToken cancellationToken = default
    );

    Task CreateAndSendAuditMessageAsync(
        AuditPepFindInput input,
        CancellationToken cancellationToken
    );
}
