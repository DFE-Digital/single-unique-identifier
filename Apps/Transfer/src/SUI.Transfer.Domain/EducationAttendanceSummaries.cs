using SUI.Custodians.API.Client;

namespace SUI.Transfer.Domain;

public record EducationAttendanceSummaries
{
    public required YearlyEducationAttendanceV1? CurrentAcademicYear { get; init; }

    public required YearlyEducationAttendanceV1? LastAcademicYear { get; init; }
}
