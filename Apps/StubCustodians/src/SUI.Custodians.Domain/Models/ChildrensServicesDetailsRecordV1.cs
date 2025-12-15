namespace SUI.Custodians.Domain.Models;

/// <summary>
/// Details related to the Children's Services for a specific child.
/// </summary>
public record ChildrensServicesDetailsRecordV1 : SuiRecord
{
    /// <summary>
    /// CS - Key worker
    /// </summary>
    /// <example>Alex Patel</example>
    public string? KeyWorker { get; init; }

    /// <summary>
    /// CS - Duty contact details
    /// </summary>
    /// <example>csc@bromley.gov.uk, 08792675387</example>
    public IReadOnlyCollection<string>? DutyContactDetails { get; init; }

    /// <summary>
    /// CS - Team involvement
    /// </summary>
    /// <example>Disabled Childrens Team, Neighbourhood Team</example>
    public IReadOnlyCollection<string>? TeamInvolvement { get; init; }

    /// <summary>
    /// CS - Referrals
    /// </summary>
    public IReadOnlyCollection<ChildrensServicesReferralV1>? ChildrensServicesReferrals { get; init; }
}
