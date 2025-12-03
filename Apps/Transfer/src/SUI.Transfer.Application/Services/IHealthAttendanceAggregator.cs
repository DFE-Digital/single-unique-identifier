using SUI.Transfer.Domain;

namespace SUI.Transfer.Application.Services;

public interface IHealthAttendanceAggregator
{
    HealthAttendanceSummaries? ApplyAggregation(ConsolidatedData consolidatedData);
}
