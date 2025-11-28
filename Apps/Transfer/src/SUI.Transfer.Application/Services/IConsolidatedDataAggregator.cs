using SUI.Transfer.Domain;

namespace SUI.Transfer.Application.Services;

public interface IConsolidatedDataAggregator
{
    AggregatedConsolidatedData ApplyAggregations(ConsolidatedData consolidatedData);
}
