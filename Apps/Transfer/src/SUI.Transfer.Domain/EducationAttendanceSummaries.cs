using SUI.Custodians.API.Client;

namespace SUI.Transfer.Domain;

public record EducationAttendanceSummaries
{
    public required EducationAttendanceV1? CurrentAcademicYear { get; init; }

    public required EducationAttendanceV1? LastAcademicYear { get; init; }
}
