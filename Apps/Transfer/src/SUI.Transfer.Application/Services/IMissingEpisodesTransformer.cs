using SUI.Transfer.Domain;

namespace SUI.Transfer.Application.Services;

public interface IMissingEpisodesTransformer
{
    ChildrensServicesReferralSummaries? ApplyTransformation(ConsolidatedData consolidatedData);
}
