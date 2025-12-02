namespace SUI.Custodians.Domain.Models;

public record ChildSocialCareReferralV1
{
    /// <summary>
    /// CSC - Referral history - Date
    /// </summary>
    /// <example>2025-08-16</example>
    public DateOnly? Date { get; init; }

    /// <summary>
    /// CSC - Referral history - Type
    /// </summary>
    /// <example>Early Help</example>
    public string? Type { get; init; }
}
