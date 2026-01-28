namespace SUI.Custodians.Domain.Models;

public record ChildrensServicesReferral
{
    /// <summary>
    /// Children's Services - Referral history - Date
    /// </summary>
    /// <example>2025-08-16</example>
    public DateOnly? Date { get; init; }

    /// <summary>
    /// Children's Services - Referral history - Type
    /// </summary>
    /// <example>Early Help</example>
    public string? Type { get; init; }
}
