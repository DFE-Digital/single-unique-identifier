namespace SUI.Custodians.Domain.Models;

/// <summary>
/// Details related to the Children's Social Care and other Children's Services for a specific child.
/// </summary>
public record ChildSocialCareDetailsRecordV1 : SuiRecord
{
    /// <summary>
    /// CSC - Key worker
    /// </summary>
    /// <example>Alex Patel</example>
    public string? KeyWorker { get; init; }

    /// <summary>
    /// CSC - Duty contact details
    /// </summary>
    /// <example>csc@bromley.gov.uk, 08792675387</example>
    public IReadOnlyCollection<string>? DutyContactDetails { get; init; }

    /// <summary>
    /// CSC - Team involvement
    /// </summary>
    /// <example>Disabled Childrens Team, Neighbourhood Team</example>
    public IReadOnlyCollection<string>? TeamInvolvement { get; init; }

    /// <summary>
    /// Child Social Care Referrals
    /// </summary>
    public IReadOnlyCollection<ChildSocialCareReferralV1>? CSCReferrals { get; init; }
}
