using SUI.Transfer.Domain;

namespace SUI.Transfer.Application.Services;

public interface IEducationAttendanceTransformer
{
    EducationAttendanceSummaries? ApplyTransformation(ConsolidatedData consolidatedData);
}
