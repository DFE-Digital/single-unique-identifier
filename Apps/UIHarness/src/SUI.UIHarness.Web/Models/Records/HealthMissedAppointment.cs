namespace SUI.UIHarness.Web.Models.Records;

public record HealthMissedAppointment
{
    /// <summary>
    /// Health - Missed appointments history - Date
    /// </summary>
    /// <example>2025-07-24</example>
    public DateOnly? Date { get; init; }

    /// <summary>
    /// Health - Missed appointments history - Setting
    /// </summary>
    /// <example>GP</example>
    public HealthSetting? Setting { get; init; }

    /// <summary>
    /// Health - Missed appointments history - Reason
    /// </summary>
    /// <example>School Examination</example>
    public string? Reason { get; init; }
}
