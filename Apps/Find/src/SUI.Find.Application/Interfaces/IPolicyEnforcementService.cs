using SUI.Find.Application.Models;
using SUI.Find.Application.Models.Pep;

namespace SUI.Find.Application.Interfaces;

public interface IPolicyEnforcementService
{
    Task<PolicyDecisionResult> EvaluateAsync(
        PolicyDecisionRequest request,
        DsaPolicyDefinition dsaPolicy,
        string destOrgType,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Decides which of the specified items can be shared from the Source Organization (Custodian of data being shared) to the Destination Organization (Searcher/Requestor).
    /// This method is intended to be used for PEP filtering in any context.
    /// </summary>
    /// <param name="sourceOrgId">Organization ID of the Custodian that owns the data being shared.</param>
    /// <param name="destOrgId">Organization ID performing the search or making the request for the data, i.e. who the data is being shared to.</param>
    /// <param name="destOrgType">Organization Type of the Searcher/Requestor.</param>
    /// <param name="pepFilterableItems">The items that represent the data being shared from the Source Organization (Custodian of data being shared) to the Destination Organization (Searcher/Requestor).</param>
    /// <param name="dsaPolicy">The Data Sharing Agreement policy belonging to the Source Organization (Custodian of data being shared).</param>
    /// <param name="purpose">The reason why the Searcher/Requestor requested the data, and why they need to see the data.</param>
    /// <param name="cancellationToken">The cancellation token for the async context.</param>
    /// <returns>The specified items with associated data sharing decision.</returns>
    Task<IReadOnlyList<PepResultItem<TItem>>> FilterItemsAsync<TItem>(
        string sourceOrgId,
        string destOrgId,
        string destOrgType,
        IReadOnlyList<TItem> pepFilterableItems,
        DsaPolicyDefinition dsaPolicy,
        string purpose,
        CancellationToken cancellationToken = default
    )
        where TItem : IPepFilterable;

    /// <summary>
    /// Decides which of the specified results can be shared from the Source Organization (Custodian of data being shared) to the Destination Organization (Searcher).
    /// This method is intended to be used specifically for PEP filtering searches.
    /// </summary>
    /// <param name="sourceOrgId">Organization ID of the Custodian that owns the data being shared.</param>
    /// <param name="destOrgId">Organization ID performing the search, i.e. who the data is being shared to.</param>
    /// <param name="destOrgType">Organization Type of the Searcher.</param>
    /// <param name="searchResultItems">The data being shared from the Source Organization (Custodian of data being shared) to the Destination Organization (Searcher).</param>
    /// <param name="dsaPolicy">The Data Sharing Agreement policy belonging to the Source Organization (Custodian of data being shared).</param>
    /// <param name="purpose">The reason why the Searcher performed the search, and why they need to see the data.</param>
    /// <param name="cancellationToken">The cancellation token for the async context.</param>
    /// <returns>The specified search results with associated data sharing decision.</returns>
    Task<IReadOnlyList<SearchResultWithDecision>> FilterResultsAsync(
        string sourceOrgId,
        string destOrgId,
        string destOrgType,
        IReadOnlyList<CustodianSearchResultItem> searchResultItems,
        DsaPolicyDefinition dsaPolicy,
        string purpose,
        CancellationToken cancellationToken = default
    );
}
