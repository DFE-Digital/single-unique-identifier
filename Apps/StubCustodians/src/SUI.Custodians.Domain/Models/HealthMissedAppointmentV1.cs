namespace SUI.Custodians.Domain.Models;

public record HealthMissedAppointmentV1
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
    public HealthSettingV1? Setting { get; init; }
}
