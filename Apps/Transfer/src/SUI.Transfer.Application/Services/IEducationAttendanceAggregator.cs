using SUI.Transfer.Domain;

namespace SUI.Transfer.Application.Services;

public interface IEducationAttendanceAggregator
{
    EducationAttendanceSummaries? ApplyAggregation(ConsolidatedData consolidatedData);
}
