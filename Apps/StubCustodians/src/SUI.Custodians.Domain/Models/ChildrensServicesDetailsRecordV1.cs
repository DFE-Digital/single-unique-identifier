namespace SUI.Custodians.Domain.Models;

/// <summary>
/// Details related to the Children's Services for a specific child.
/// </summary>
public record ChildrensServicesDetailsRecordV1 : SuiRecord
{
    /// <summary>
    /// Children's Services - Key worker
    /// </summary>
    /// <example>Alex Patel</example>
    public string? KeyWorker { get; init; }

    /// <summary>
    /// Children's Services - Duty contact details
    /// </summary>
    /// <example>csc@bromley.gov.uk, 08792675387</example>
    public IReadOnlyCollection<string>? DutyContactDetails { get; init; }

    /// <summary>
    /// Children's Services - Team involvement
    /// </summary>
    /// <example>Disabled Childrens Team, Neighbourhood Team</example>
    public IReadOnlyCollection<string>? TeamInvolvement { get; init; }

    /// <summary>
    /// Children's Services - Referrals
    /// </summary>
    public IReadOnlyCollection<ChildrensServicesReferralV1>? ChildrensServicesReferrals { get; init; }
}
