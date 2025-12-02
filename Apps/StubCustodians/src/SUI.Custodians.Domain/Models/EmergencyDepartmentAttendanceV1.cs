namespace SUI.Custodians.Domain.Models;

public record EmergencyDepartmentAttendanceV1
{
    /// <summary>
    /// Health - Emergency department attendance - Date
    /// </summary>
    /// <example>2025-06-12</example>
    public DateOnly? Date { get; init; }
}
