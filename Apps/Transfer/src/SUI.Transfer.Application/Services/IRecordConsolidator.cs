using SUI.Transfer.Domain;

namespace SUI.Transfer.Application.Services;

public interface IRecordConsolidator
{
    ConsolidatedData ConsolidateRecords(UnconsolidatedData unconsolidatedData);
}
