using SUI.Transfer.Domain;

namespace SUI.Transfer.Application.Services;

public class MissingEpisodesTransformer : IMissingEpisodesTransformer
{
    public ChildrensServicesReferralSummaries? ApplyTransformation(
        ConsolidatedData consolidatedData
    )
    {
        return null;
    }
}
