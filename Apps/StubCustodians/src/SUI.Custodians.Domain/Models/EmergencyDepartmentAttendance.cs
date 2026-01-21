namespace SUI.Custodians.Domain.Models;

public record EmergencyDepartmentAttendance
{
    /// <summary>
    /// Health - Emergency department attendance - Date
    /// </summary>
    /// <example>2025-06-12</example>
    public DateOnly? Date { get; init; }

    /// <summary>
    /// Health - Emergency department attendance - Reason
    /// </summary>
    /// <example>Minor road accident</example>
    public string? Reason { get; init; }
}
