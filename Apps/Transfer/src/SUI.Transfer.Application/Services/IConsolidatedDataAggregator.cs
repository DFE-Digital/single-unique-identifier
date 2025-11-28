using SUI.Transfer.Domain;

namespace SUI.Transfer.Application.Services;

public interface IConsolidatedDataAggregator
{
    AggregatedData ApplyAggregations(ConsolidatedData consolidatedData);
}
