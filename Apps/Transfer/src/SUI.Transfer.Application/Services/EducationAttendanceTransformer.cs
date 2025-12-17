using SUI.Custodians.API.Client;
using SUI.Transfer.Domain;
using SUI.Transfer.Domain.Services;

namespace SUI.Transfer.Application.Services;

public class EducationAttendanceTransformer(TimeProvider timeProvider)
    : IEducationAttendanceTransformer
{
    public EducationAttendanceSummaries? ApplyTransformation(ConsolidatedData consolidatedData)
    {
        if (
            consolidatedData.EducationDetailsRecord?.YearlyEducationAttendances.Value is null
            || consolidatedData.EducationDetailsRecord.YearlyEducationAttendances.Value.Count.Equals(
                0
            )
        )
            return null;

        var currentAcademicYear = GetCurrentAcademicYearStart();

        return new EducationAttendanceSummaries
        {
            CurrentAcademicYear = GetAttendanceForYearStart(
                consolidatedData.EducationDetailsRecord,
                currentAcademicYear
            ),
            LastAcademicYear = GetAttendanceForYearStart(
                consolidatedData.EducationDetailsRecord,
                currentAcademicYear - 1
            ),
        };
    }

    private static YearlyEducationAttendanceV1? GetAttendanceForYearStart(
        EducationDetailsRecordV1Consolidated educationDetailsRecord,
        int academicYearStart
    )
    {
        return educationDetailsRecord.YearlyEducationAttendances.Value?.FirstOrDefault(x =>
            x.AcademicTermYearStart == academicYearStart
            && x.AcademicTermYearEnd == academicYearStart + 1
        );
    }

    private int GetCurrentAcademicYearStart()
    {
        var now = timeProvider.GetUtcNow();
        if (now.Month >= 9)
            return now.Year;

        return now.Year - 1;
    }
}
