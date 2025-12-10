using SUI.Transfer.Domain;

namespace SUI.Transfer.Application.Services;

public interface IChildServicesReferralAggregator
{
    ChildServicesReferralSummaries? ApplyAggregation(ConsolidatedData consolidatedData);
}
