namespace SUI.Custodians.Domain.Models;

/// <summary>
/// Education related data about a specific child.
/// </summary>
public record EducationDetailsRecord : SuiRecord
{
    /// <summary>
    /// Education setting - Name
    /// </summary>
    /// <example>Redwood Academy</example>
    public string? EducationSettingName { get; init; }

    /// <summary>
    /// Education setting - Address
    /// </summary>
    /// <example>Redrood Drive, Maltby, London, SE6 8DL</example>
    public Address? EducationSettingAddress { get; init; }

    /// <summary>
    /// Education setting - Telephone
    /// </summary>
    /// <example>0123456789</example>
    public string? EducationSettingTelephone { get; init; }

    /// <summary>
    /// Yearly Education Attendances
    /// </summary>
    public IReadOnlyCollection<YearlyEducationAttendance>? YearlyEducationAttendances { get; init; }
}
