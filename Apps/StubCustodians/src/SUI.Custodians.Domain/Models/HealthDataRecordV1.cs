namespace SUI.Custodians.Domain.Models;

public record HealthDataRecordV1 : SuiRecord
{
    /// <summary>
    /// Registered GP
    /// </summary>
    /// <example>Dr E Green</example>
    public string? RegisteredGP { get; init; }

    /// <summary>
    /// GP Address
    /// </summary>
    /// <example>Duke Medical Centre, 28 Talbot Road, Sheffield, S2 2TD</example>
    public AddressV1? GPAddress { get; init; }

    /// <summary>
    /// GP Telephone
    /// </summary>
    /// <example>0114 272 2100</example>
    public string? GPTelephone { get; init; }

    /// <summary>
    /// Child and Adolescent Mental Health Services - Contact address
    /// </summary>
    public AddressV1? CAMHSContactAddress { get; init; }

    /// <summary>
    /// Child and Adolescent Mental Health Services - Contact telephone
    /// </summary>
    /// <example>01422 345926</example>
    public string? CAMHSContactTelephone { get; init; }

    /// <summary>
    /// Missed Healthcare Appointments
    /// </summary>
    public IReadOnlyCollection<HealthMissedAppointmentV1>? MissedAppointments { get; init; }

    /// <summary>
    /// Emergency (A&E) Department Attendances
    /// </summary>
    public IReadOnlyCollection<EmergencyDepartmentAttendanceV1>? EmergencyDepartmentAttendances { get; init; }
}
