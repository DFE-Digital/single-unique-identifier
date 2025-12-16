namespace SUI.Custodians.Domain.Models;

public record ChildrensServicesReferralV1
{
    /// <summary>
    /// CS - Referral history - Date
    /// </summary>
    /// <example>2025-08-16</example>
    public DateOnly? Date { get; init; }

    /// <summary>
    /// CS - Referral history - Type
    /// </summary>
    /// <example>Early Help</example>
    public string? Type { get; init; }
}
