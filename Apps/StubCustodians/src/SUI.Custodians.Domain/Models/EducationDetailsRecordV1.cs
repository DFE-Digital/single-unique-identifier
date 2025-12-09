namespace SUI.Custodians.Domain.Models;

public record EducationDetailsRecordV1 : SuiRecord
{
    /// <summary>
    /// Education setting - Name
    /// </summary>
    /// <example>Redwood Academy</example>
    public string? EducationSettingName { get; init; }

    public AddressV1? EducationSettingAddress { get; init; }

    /// <summary>
    /// Education setting - Telephone
    /// </summary>
    /// <example>0123456789</example>
    public string? EducationSettingTelephone { get; init; }

    /// <summary>
    /// Education Attendances
    /// </summary>
    public IReadOnlyCollection<EducationAttendanceV1>? EducationAttendances { get; init; }
}
