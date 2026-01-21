namespace SUI.Custodians.Domain.Models;

public record YearlyEducationAttendance
{
    /// <summary>
    /// Education attendance history - Academic Term Year Start
    /// </summary>
    /// <example>2024</example>
    public int? AcademicTermYearStart { get; init; }

    /// <summary>
    /// Education attendance history - Academic Term Year End
    /// </summary>
    /// <example>2025</example>
    public int? AcademicTermYearEnd { get; init; }

    /// <summary>
    /// Education attendance history - Attendance Percentage
    /// </summary>
    /// <example>70</example>
    public float? AttendancePercentage { get; init; }

    /// <summary>
    /// Education attendance history - Unauthorised Absence Percentage
    /// </summary>
    /// <example>2</example>
    public float? UnauthorisedAbsencePercentage { get; init; }

    /// <summary>
    /// Education attendance history - Suspensions
    /// </summary>
    /// <example>1</example>
    public int? Suspensions { get; init; }

    /// <summary>
    /// Education attendance history - Exclusions
    /// </summary>
    /// <example>0</example>
    public int? Exclusions { get; init; }

    /// <summary>
    /// Education attendance history - School moves non transitional
    /// </summary>
    /// <example>0</example>
    public int? SchoolMovesNonTransitional { get; init; }

    /// <summary>
    /// Education attendance history - School's average attendance
    /// </summary>
    /// <example>97.5</example>
    public float? SchoolsAverageAttendance { get; init; }
}
