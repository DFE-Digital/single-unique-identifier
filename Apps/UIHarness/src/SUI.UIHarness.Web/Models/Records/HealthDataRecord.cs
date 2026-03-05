namespace SUI.UIHarness.Web.Models.Records;

/// <summary>
/// Details related to Healthcare about a specific child.
/// </summary>
public record HealthDataRecord : SuiRecord
{
    /// <summary>
    /// Registered GP Name
    /// </summary>
    /// <example>Dr E Green</example>
    public string? RegisteredGPName { get; init; }

    /// <summary>
    /// Registered GP Surgery
    /// </summary>
    /// <example>Duke Medical Centre</example>
    public string? RegisteredGPSurgery { get; init; }

    /// <summary>
    /// GP Address
    /// </summary>
    /// <example>Duke Medical Centre, 28 Talbot Road, Sheffield, S2 2TD</example>
    public Address? GPAddress { get; init; }

    /// <summary>
    /// GP Telephone
    /// </summary>
    /// <example>0114 272 2100</example>
    public string? GPTelephone { get; init; }

    /// <summary>
    /// Child and Adolescent Mental Health Services - Contact Details
    /// </summary>
    /// <example>01422 345926, camhs.dukemc@gmail.com</example>
    public IReadOnlyCollection<string>? CAMHSContactDetails { get; init; }

    /// <summary>
    /// Child and Adolescent Mental Health Services - Team Involvement
    /// </summary>
    /// <example>Minds Matter Clinic, Duke Mental Aid Clinic</example>
    public IReadOnlyCollection<string>? CAMHSTeamInvolvement { get; init; }

    /// <summary>
    /// Missed Healthcare Appointments
    /// </summary>
    public IReadOnlyCollection<HealthMissedAppointment>? MissedAppointments { get; init; }

    /// <summary>
    /// Emergency (A&amp;E) Department Attendances
    /// </summary>
    public IReadOnlyCollection<EmergencyDepartmentAttendance>? EmergencyDepartmentAttendances { get; init; }
}
